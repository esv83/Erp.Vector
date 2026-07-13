using CaSoft.Connectors.GpsGate;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Repositories;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Api.Workers;
using EmergencyPlatformConnector;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// ── Auth Keycloak (JWT) — gated par config, comme Crew.Api (MOB-4a) ──────────
var keycloakEnabled = builder.Configuration.GetValue("Keycloak:Enabled", false);
if (keycloakEnabled)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = "https://auth.ade-dev.fr/realms/delesse"; //builder.Configuration["Keycloak:Authority"];
            options.Audience = "us-ambulance";//builder.Configuration["Keycloak:Audience"];
            options.RequireHttpsMetadata = builder.Configuration.GetValue("Keycloak:RequireHttpsMetadata", true);

            // Conserver les claims JWT bruts (sub, iss, exp…) : sinon .NET remappe par défaut « sub »
            // vers ClaimTypes.NameIdentifier (URI XML long), et toute lecture de « sub » (log ci-dessous,
            // GetKeycloakSubject) tombe à vide → « impossible d'extraire le user id du sub ». On lit le
            // sub tel que Keycloak l'émet. (Correctif canonique Keycloak + ASP.NET Core.)
            options.MapInboundClaims = false;

            // ⚠️ DEV/TEST UNIQUEMENT (Keycloak:DisableValidation=true) : décode le token et lit les
            // claims (sub…) SANS vérifier signature / issuer / audience / expiration, et SANS contacter
            // Keycloak. Permet de tester la lecture du sub quand l'Authority n'est pas joignable.
            // NE JAMAIS activer en production — désactivé par défaut.
            var disableValidation = builder.Configuration.GetValue("Keycloak:DisableValidation", false);
            if (disableValidation)
            {
                options.Authority = null; // évite le fetch OIDC metadata (placeholder)
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    SignatureValidator = (token, _) => new JsonWebToken(token)
                };
            }

            // Audience : Keycloak émet par défaut « aud: account » (aucun audience mapper configuré sur
            // le realm). Plutôt que d'imposer un mapper côté Keycloak, on désactive la validation d'aud
            // et on valide l'« azp » (authorized party = client émetteur du token) dans OnTokenValidated.
            // Signature / issuer / expiration restent validés. (Si un audience mapper est ajouté plus
            // tard, on pourra revenir à une validation d'audience stricte.)
            options.TokenValidationParameters.ValidateAudience = false;

            // ── Interception JWT observable/testable (MOB-4a) ────────────────────
            // On trace chaque étape du pipeline et on dépose la raison d'un rejet
            // dans HttpContext.Items pour que les controllers renvoient une erreur explicite.
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var log = ctx.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>().CreateLogger("Keycloak.Jwt");
                    log.LogDebug("JWT — requête {Method} {Path} : header Authorization {Present}.",
                        ctx.HttpContext.Request.Method, ctx.HttpContext.Request.Path,
                        ctx.HttpContext.HasAuthorizationHeader() ? "présent" : "ABSENT");
                    return Task.CompletedTask;
                },
                OnTokenValidated = ctx =>
                {
                    var log = ctx.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>().CreateLogger("Keycloak.Jwt");

                    // Cloisonnement API (remplace la validation d'aud) : le token doit avoir été émis POUR
                    // le client mobile. On l'exige via « azp ». Ignoré en mode DisableValidation (dev pur).
                    const string expectedAzp = "us-ambulance"; // aligné sur options.Audience (cf. hardcode, doc §1.2)
                    var azp = ctx.Principal?.FindFirst("azp")?.Value;
                    if (!disableValidation && !string.Equals(azp, expectedAzp, StringComparison.Ordinal))
                    {
                        var reason = $"azp '{azp}' non autorisé (attendu '{expectedAzp}').";
                        // Déposé pour lecture par les controllers (cf. MobileCallerExtensions.GetJwtError).
                        ctx.HttpContext.Items[MobileCallerExtensions.JwtErrorKey] = reason;
                        log.LogWarning("JWT REJETÉ sur {Path} : {Reason}.", ctx.HttpContext.Request.Path, reason);
                        ctx.Fail(reason);
                        return Task.CompletedTask;
                    }

                    log.LogInformation("JWT validé : sub={Sub} azp={Azp} iss={Iss} aud={Aud} exp={Exp}.",
                        ctx.Principal?.FindFirst("sub")?.Value,
                        azp,
                        ctx.Principal?.FindFirst("iss")?.Value,
                        ctx.Principal?.FindFirst("aud")?.Value,
                        ctx.Principal?.FindFirst("exp")?.Value);
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = ctx =>
                {
                    var reason = $"{ctx.Exception.GetType().Name}: {ctx.Exception.Message}";
                    // Déposé pour lecture par les controllers (cf. MobileCallerExtensions.GetJwtError).
                    ctx.HttpContext.Items[MobileCallerExtensions.JwtErrorKey] = reason;
                    var log = ctx.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>().CreateLogger("Keycloak.Jwt");
                    log.LogWarning(ctx.Exception, "JWT REJETÉ sur {Path} : {Reason}.",
                        ctx.HttpContext.Request.Path, reason);
                    return Task.CompletedTask;
                }
            };
        });
    builder.Services.AddAuthorization();
}

// BD Mobile dédiée (MOB_* : sessions, timeline statuts, signatures)
builder.Services.AddDbContext<MobileDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MobileDb")));

// BaseAddress DOIT finir par '/' : sinon la résolution d'URI relative supprime le dernier
// segment de la base (ex. ".../order" + "personnel/x" => ".../personnel/x" — le "/order" est
// perdu), l'appel part au mauvais endroit et Orders.Api renvoie un non-2xx → 500.
static Uri OrdersBaseUri(IConfiguration cfg)
{
    var raw = cfg["OrdersApi:BaseUrl"]
        ?? throw new InvalidOperationException("OrdersApi:BaseUrl manquant.");
    return new Uri(raw.EndsWith('/') ? raw : raw + "/");
}

// Découplage Vector↔Orders (4a) : données de référence ERP lues via Orders.Api en HTTP
// (missions, commandes, bénéficiaires, équipages), comme Address.Api. Plus aucune réf projet Orders.
builder.Services.AddHttpClient<IErpReadApiClient, HttpErpReadApiClient>(c =>
    c.BaseAddress = OrdersBaseUri(builder.Configuration));

// TRF-5 : chemin d'écriture Vector→Orders (projection de l'avancement opérationnel terrain).
builder.Services.AddHttpClient<IErpWriteApiClient, HttpErpWriteApiClient>(c =>
    c.BaseAddress = OrdersBaseUri(builder.Configuration));

// Contrat mobile inchangé : PascalCase comme l'ancienne WebApi (pas de camelCase).
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "USVector — API terrain",
        Version = "v2.0",
        Description = "USVector : API terrain ambulanciers reconnectée à l'ERP (contrat legacy préservé)"
    });

    // Bouton « Authorize » : coller un JWT → envoyé en « Authorization: Bearer <token> » sur les requêtes.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Collez le JWT (sans le préfixe « Bearer »)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── Ports BD Mobile → implémentations réelles (MOB-2) ────────────────────────
builder.Services.AddScoped<ISignatureRepository, SignatureRepository>();
builder.Services.AddScoped<IJobTimeRepository, JobTimeRepository>();
// Synchro régulation garantie : worker qui vide l'Outbox de projection opérationnelle (debounce + retry).
builder.Services.AddHostedService<OperationalOutboxDispatcher>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
// MOB-13 : overlay attributs de mission (catalogue + valeurs en BD Mobile).
builder.Services.AddScoped<IJobAttributeOverlay, JobAttributeOverlayRepository>();
// Carte mutuelle (P1) : stockage BD Mobile.
builder.Services.AddScoped<IMutuelleCardRepository, MutuelleCardRepository>();
// Anomalies terrain (TRF-8) : stockage BD Mobile.
builder.Services.AddScoped<IAnomalyRepository, AnomalyRepository>();
// Documents/photos terrain (TRF-10) : stockage BD Mobile.
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
// Paquet d'enrichissement consolidé (TRF-6) : tiré par Certification au transfert.
builder.Services.AddScoped<IFieldDataReader, CaSoft.Erp.USVector.Infrastructure.Repositories.FieldDataReader>();

// ── Ports ERP-backed (in-process) ───────────────────────────────────────────
builder.Services.AddScoped<ICrewRepository, CaSoft.Erp.USVector.Infrastructure.Repositories.Erp.CrewRepository>();
// MOB-6 : détail mission (mission + commande + bénéficiaire ERP → ClJob).
builder.Services.AddScoped<IJobRepository, CaSoft.Erp.USVector.Infrastructure.Repositories.Erp.JobRepository>();
// MOB-4a : résolution identité Keycloak (sub → PER_ID → crews actifs).
// Décoré par un cache (chemin chaud : garde-fou crew de chaque requête). TTLs configurables :
// - PersonnelMinutes : mapping sub→PER_ID quasi-immuable → long (défaut 8h).
// - ActiveCrewsMinutes : crews actifs volatils intra-journée → court (défaut 15 min) ;
//   la (re)sélection /api/crew/mine lit frais (bypass) → crew créé le jour même visible aussitôt.
builder.Services.AddMemoryCache();
builder.Services.AddScoped<CaSoft.Erp.USVector.Infrastructure.Repositories.Erp.MobileIdentityResolver>();
builder.Services.AddScoped<IMobileIdentityResolver>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>().GetSection("MobileIdentityCache");
    var personnelTtl = TimeSpan.FromMinutes(cfg.GetValue<double?>("PersonnelMinutes") ?? 480);
    var activeCrewsTtl = TimeSpan.FromMinutes(cfg.GetValue<double?>("ActiveCrewsMinutes") ?? 15);
    return new CaSoft.Erp.USVector.Infrastructure.Repositories.Erp.CachingMobileIdentityResolver(
        sp.GetRequiredService<CaSoft.Erp.USVector.Infrastructure.Repositories.Erp.MobileIdentityResolver>(),
        sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
        personnelTtl, activeCrewsTtl);
});

// Diagnostic développeur (dev-only) de la chaîne d'identité (sub → PER_ID → crews actifs). Cf. DiagController.
builder.Services.AddScoped<CaSoft.Erp.USVector.Infrastructure.Diagnostics.CrewChainDiagnostic>();
// Support diag : résolution username → sub via l'Admin API Keycloak (service account, dev-only).
builder.Services.AddHttpClient<CaSoft.Erp.USVector.Infrastructure.Diagnostics.KeycloakAdminClient>();

// ── Ports ERP → stubs (remplacés itération par itération, MOB-4+) ────────────
builder.Services.AddScoped<ILogRepository, LogRepositoryStub>();
builder.Services.AddScoped<ILogAnalyzeRepository, LogAnalyzeRepositoryStub>();
builder.Services.AddScoped<IContactRepository, ContactRepositoryStub>();
builder.Services.AddScoped<IInvoicingRepository, InvoicingRepositoryStub>();
builder.Services.AddScoped<IMissionRepositary, MissionRepositoryStub>();

// ── Services applicatifs (portés tels quels de MobApp.Application) ───────────
builder.Services.AddScoped<ICrewCache, ClCrewListCache>();
builder.Services.AddScoped<IJobCache, ClJobListCache>();
builder.Services.AddScoped<IJobService, ClJobService>();
builder.Services.AddScoped<ICrewService, ClCrewService>();

// ── Connecteurs Sirus (UDP) + GpsGate (REST) — statu quo, config externalisée ─
builder.Services.AddScoped<IEmergencyConnector>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();

    IEmergencyPlatform platform = ClSirusClient.GetBuilder()
        .WithServer(new ClServerAddress(
            config["Sirus:Host"]!,
            config.GetValue<int>("Sirus:Port")))
        .Build();

    IGeolocServer geoloc = ClGpsGateClient.GetBuilder()
        .WithServer(config["GpsGate:Server"]!)
        .WithAppId(config.GetValue<int>("GpsGate:AppId"))
        .WithViewId(config.GetValue<int>("GpsGate:ViewId"))
        .WithCredential(config["GpsGate:User"]!, config["GpsGate:Password"]!)
        .Build();

    return new ClEmergencyPlatformService(platform, geoloc);
});

// NLog
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// CORS — TODO : restreindre (aligné sur le P0 #2 de l'ERP)
builder.Services.AddCors();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // Endpoint relatif : fonctionne à la racine ET sous un sous-chemin IIS (path base)
    c.SwaggerEndpoint("v2/swagger.json", "USVector API v2");
    c.RoutePrefix = "swagger";
});

app.UseCors(options => options
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowAnyOrigin()
);

if (keycloakEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllers();

app.Run();
