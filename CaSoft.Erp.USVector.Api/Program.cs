using CaSoft.Framework.Application;
using CaSoft.Connectors.GpsGate;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Repositories;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Ocr;
using CaSoft.Erp.USVector.Api.Infrastructure;
using CaSoft.Erp.USVector.Api.Workers;
using EmergencyPlatformConnector;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Mode formation (Simulation:Enabled) : missions fictives pour les équipages en formation, et personas sans
// compte pour le développement de l'app (Simulation:AnonymousPersonas, refusé en Production).
var simulation = builder.Services.AddVectorSimulationOptions(builder.Configuration, builder.Environment);

// ── Auth Keycloak (JWT) — gated par config, comme Crew.Api (MOB-4a) ──────────
// Itération 11 : anciennes clés (Audience, ServiceAzp, ScreenAzp) recopiées sous les noms du socle.
var keycloakLegacyKeys = KeycloakLegacyKeys.Apply(builder.Configuration);
var keycloakEnabled = builder.Configuration.GetValue("Keycloak:Enabled", false);
if (keycloakEnabled)
{
    // KC-1 — realm et client mobile viennent de la config (plus aucune valeur en dur ci-dessous).
    var authority = builder.Configuration["Keycloak:Authority"];
    // Le client mobile : le « front » du socle, et l'azp qu'exige la politique de repli.
    var audience = ClKeycloakCallers.ReadMobileAzp(builder.Configuration);
    // ⚠️ DEV/TEST UNIQUEMENT : décode le token SANS vérifier signature / issuer / audience /
    // expiration / azp, et SANS contacter Keycloak. NE JAMAIS activer en production.
    var disableValidation = builder.Configuration.GetValue("Keycloak:DisableValidation", false);
    // C2 — modules de service (la facturation) : n'entrent que sur ServiceOrMobilePolicy.
    var serviceAzp = ClKeycloakCallers.ReadServiceAzp(builder.Configuration);
    // Écrans de régulation et de certification (04/10) : lecture de la carte mutuelle seulement.
    var screenAzp = ClKeycloakCallers.ReadScreenAzp(builder.Configuration);

    // Garde-fou AU DÉMARRAGE (KC-1) : une Authority restée au placeholder fait échouer le fetch OIDC
    // et rejette SILENCIEUSEMENT tous les tokens. Le socle refuse déjà une Authority ou un ClientId
    // absents ; le placeholder, lui, est propre à l'historique de Vector.
    if (!disableValidation && authority?.Contains("placeholder", StringComparison.OrdinalIgnoreCase) == true)
        throw new InvalidOperationException(
            $"Keycloak:Authority invalide ('{authority}'). Renseigner le realm dans "
            + "appsettings.{Environnement}.json ou via la variable d'environnement Keycloak__Authority.");

    // Itération 11 — la validation est celle du socle : émetteur = Authority, MapInboundClaims = false,
    // pas de contrôle d'audience (Keycloak émet « aud: account »), azp admis = ClientId +
    // FrontClientIds + AllowedAzp, refus motivé sinon.
    // Nom complet : le socle a aussi un ClKeycloakCallers (l'admission), homonyme du nôtre (les routes).
    CaSoft.Framework.Security.ClKeycloakAuthenticationExtensions.AddCaSoftKeycloakJwtBearer(
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme), builder.Configuration);

    // Ce que Vector ajoute autour, sans rien retirer au socle : le motif d'un rejet déposé pour les
    // contrôleurs (MobileCallerExtensions.GetJwtError), le journal « Keycloak.Jwt », et le mode dégradé.
    builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
        options => VectorJwtBearerEvents.Apply(options, disableValidation, simulation, audience));
    // ⚠️ FERMÉ PAR DÉFAUT. Jusqu'ici aucun contrôleur ne portait d'attribut d'autorisation et il
    // n'existait aucune politique globale : seuls les cinq endpoints passant par CrewAccess étaient
    // protégés, par leur code. Tout le reste répondait 200 à qui connaissait un identifiant de
    // mission — y compris la structure du formulaire, VALEURS COMPRISES (date de naissance, NIR).
    //
    // La politique de repli renverse la charge : une route est protégée sauf mention contraire. Cela
    // ferme la classe entière du problème, et pas seulement les cas connus — un contrôleur ajouté
    // demain naît protégé.
    //
    // Elle n'est posée que si Keycloak est actif : sans schéma d'authentification, elle rejetterait
    // tout, y compris en développement local où la validation est volontairement désactivée.
    //
    // C2 — la politique de repli exige en plus l'azp MOBILE : un jeton de service accepté à
    // l'authentification (la facturation) n'entre que sur les routes qui portent
    // ClKeycloakCallers.ServiceOrMobilePolicy. En DisableValidation (dev pur), aucun azp n'est garanti :
    // seule l'authentification reste exigée, comme avant.
    var mobileAzpEnforced = !disableValidation && !string.IsNullOrWhiteSpace(audience);
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = mobileAzpEnforced
            ? ClKeycloakCallers.MobileOnly(audience!)
            : new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

        options.AddPolicy(ClKeycloakCallers.ServiceOrMobilePolicy, mobileAzpEnforced
            ? ClKeycloakCallers.ServiceOrMobile(audience!, serviceAzp)
            : new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        options.AddPolicy(ClKeycloakCallers.MutuelleCardReadPolicy, mobileAzpEnforced
            ? ClKeycloakCallers.MutuelleCardRead(audience!, serviceAzp, screenAzp)
            : new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
    });
}
else
{
    // Sans Keycloak (développement local), les politiques nommées existent quand même : une route qui
    // les porte ne doit pas faire échouer l'application au démarrage, faute de politique enregistrée.
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(ClKeycloakCallers.ServiceOrMobilePolicy, policy => policy.RequireAssertion(_ => true));
        options.AddPolicy(ClKeycloakCallers.MutuelleCardReadPolicy, policy => policy.RequireAssertion(_ => true));
    });
}

// G4 — Journal du schéma (`__VectorSchema`) : lu au démarrage, et servi par api/version/runtime.
// Sa propre connexion, hors du DbContext : il doit pouvoir dire « base injoignable ».
builder.Services.AddSingleton(new CaSoft.Erp.USVector.Infrastructure.Persistence.Schema.SchemaJournal(
    builder.Configuration.GetConnectionString("MobileDb")));
builder.Services.AddHostedService<SchemaCheckAtStartup>();

// BD Mobile dédiée (MOB_* : sessions, timeline statuts, signatures)
// Réessai EF sur les erreurs SQL passagères : absorbe une micro-coupure, pas une panne de 40 min
// (22/09). Sûr parce qu'aucune écriture n'ouvre de transaction explicite — une seule SaveChanges par
// opération. Ouvrir une transaction à la main imposera CreateExecutionStrategy().
// DR-002 — la frontière transactionnelle appartient au cas d'usage : les repositories préparent,
// IUnitOfWork.CommitAsync enregistre, une fois. Même DbContext (scoped) que les repositories.
// Celle du socle, plus la création concurrente de l'état d'une mission résolue (22/11).
builder.Services.AddScoped<IUnitOfWork>(sp =>
    new CaSoft.Erp.USVector.Infrastructure.Persistence.ClMobileUnitOfWork(sp.GetRequiredService<MobileDbContext>()));
builder.Services.AddDbContext<MobileDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MobileDb"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null)));

// BaseAddress DOIT finir par '/' : sinon la résolution d'URI relative supprime le dernier
// segment de la base (ex. ".../order" + "personnel/x" => ".../personnel/x" — le "/order" est
// perdu), l'appel part au mauvais endroit et Orders.Api renvoie un non-2xx → 500.
static Uri OrdersBaseUri(IConfiguration cfg)
{
    var raw = cfg["OrdersApi:BaseUrl"]
        ?? throw new InvalidOperationException("OrdersApi:BaseUrl manquant.");
    return new Uri(raw.EndsWith('/') ? raw : raw + "/");
}

// C2 — jeton du compte de service de Vector sur les appels à Orders.Api. INERTE tant que
// OrdersApi:ServiceAccount est incomplet (secret dans le web.config) : les appels partent sans jeton,
// comme avant. Orders reste anonyme pour le terrain ; ce jeton est le préalable à sa fermeture.
var serviceAccount = builder.Configuration.GetSection(ServiceAccountOptions.SectionName).Get<ServiceAccountOptions>()
    ?? new ServiceAccountOptions();
if (string.IsNullOrWhiteSpace(serviceAccount.TokenEndpoint)
    && !string.IsNullOrWhiteSpace(builder.Configuration["Keycloak:Authority"]))
{
    serviceAccount.TokenEndpoint = builder.Configuration["Keycloak:Authority"]!.TrimEnd('/') + "/protocol/openid-connect/token";
}
// Le gestionnaire est celui du paquet CaSoft.Identity.Client (promu depuis Vector le 13/09) : une
// source de vérité pour tous les modules. La configuration reste OrdersApi:ServiceAccount, traduite
// (cf. ServiceAccountTokenRegistration) — le web.config de production n'a rien à changer.

// Découplage Vector↔Orders (4a) : données de référence ERP lues via Orders.Api en HTTP
// (missions, commandes, bénéficiaires, équipages). Plus aucune réf projet Orders.
builder.Services.AddHttpClient<IErpReadApiClient, HttpErpReadApiClient>(c =>
    c.BaseAddress = OrdersBaseUri(builder.Configuration))
    .AddVectorServiceAccountToken(serviceAccount)
    .AddOrdersResilience(builder.Configuration);

// TRF-5 : chemin d'écriture Vector→Orders (projection de l'avancement opérationnel terrain).
builder.Services.AddHttpClient<IErpWriteApiClient, HttpErpWriteApiClient>(c =>
    c.BaseAddress = OrdersBaseUri(builder.Configuration))
    .AddVectorServiceAccountToken(serviceAccount)
    // Sûr parce que les écritures sont rejouables : instantané complet pour la projection,
    // remplacement pour le conducteur et le questionnaire (cf. OrdersResilience).
    .AddOrdersResilience(builder.Configuration);

// Confirmation de prise de service depuis l'application (lecture + confirmation). « Sans jeton »
// vaut pour le JETON DE LIEN du courriel, jamais demandé ici ; le jeton de SERVICE de Vector, lui,
// est posé comme sur les deux autres clients Orders — sans quoi ces routes tomberaient le jour où
// Orders exigera un jeton du terrain.
builder.Services.AddHttpClient<IShiftConfirmationService, HttpShiftConfirmationService>(c =>
    c.BaseAddress = OrdersBaseUri(builder.Configuration))
    .AddVectorServiceAccountToken(serviceAccount)
    // Idempotent côté Orders : un second appel rend « déjà confirmé », pas une erreur.
    .AddOrdersResilience(builder.Configuration);

// J-144 / contact — saisies de l'équipage sur le patient, par la BORDURE d'Order (pas Orders.Api) :
// Order y exige le jeton de service de Vector et juge l'auteur. Base lue à la création du client :
// une clé absente ne casse que ces deux routes, jamais le démarrage (decided.md, 20/09).
builder.Services.AddHttpClient<IBeneficiaryFieldEntryService, HttpBeneficiaryFieldEntryService>(c =>
    {
        var raw = builder.Configuration["OrdersEdge:BaseUrl"]
            ?? throw new InvalidOperationException("OrdersEdge:BaseUrl manquant.");
        c.BaseAddress = new Uri(raw.EndsWith('/') ? raw : raw + "/");
    })
    .AddVectorServiceAccountToken(serviceAccount)
    // Rejouable : l'identité se remplace, le contact n'ajoute que l'inconnu.
    .AddOrdersResilience(builder.Configuration);

// Contrat mobile inchangé : PascalCase comme l'ancienne WebApi (pas de camelCase).
// Mode formation : cantonne une requête de formation au jeu fictif (filtre global, cf. la classe).
builder.Services.AddControllers(options => options.Filters.AddService<SimulationConfinementFilter>())
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
// Synchro régulation garantie : worker qui vide l'Outbox de projection opérationnelle (debounce + retry) ;
// le cycle lui-même est en Infrastructure (AR-001). PRJ-2 — la même entrée projette aussi la signature
// (présence, motif) vers Orders : FieldProjection:Enabled, allumé le 22/11 dans appsettings.json.
builder.Services.AddSingleton(builder.Configuration.GetSection(FieldProjectionOptions.SectionName).Get<FieldProjectionOptions>()
    ?? new FieldProjectionOptions());
builder.Services.AddScoped<ClProjectionOutboxDispatcher>();
builder.Services.AddHostedService<OperationalOutboxDispatcher>();
// Motif « le patient ne peut pas signer » (MOB_012) : stockage BD Mobile, arme la projection.
builder.Services.AddScoped<ISignatureWaiverRepository, SignatureWaiverRepository>();

// Carte mutuelle (P1) : stockage BD Mobile.
builder.Services.AddScoped<IMutuelleCardRepository, MutuelleCardRepository>();
// Anomalies terrain (TRF-8) : stockage BD Mobile.
builder.Services.AddScoped<IAnomalyRepository, AnomalyRepository>();
// Documents/photos terrain (TRF-10) : stockage BD Mobile.
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
// Paquet d'enrichissement consolidé (TRF-6) : tiré par la facturation, à l'unité ou en lot (B5).
// Les silos sont lus en une requête chacun, sans binaire (FieldDataQueryService).
builder.Services.AddScoped<IFieldDataQueryService, CaSoft.Erp.USVector.Infrastructure.Repositories.FieldDataQueryService>();
builder.Services.AddScoped<IFieldDataReader, CaSoft.Erp.USVector.Infrastructure.Repositories.FieldDataReader>();
// P3 — Lecture automatique des cartes mutuelle. INERTE sans clé : le worker ne démarre pas, rien
// n'appelle de modèle, et la capture continue exactement comme avant. La décision « où tourne
// l'appel » et l'arbitrage RGPD (l'image part chez le fournisseur) précèdent l'activation.
var ocrOptions = builder.Configuration.GetSection(MutuelleCardOcrOptions.SectionName).Get<MutuelleCardOcrOptions>()
    ?? new MutuelleCardOcrOptions();
builder.Services.AddSingleton(ocrOptions);
if (ocrOptions.IsConfigured)
{
    builder.Services.AddSingleton(new Anthropic.AnthropicClient { ApiKey = ocrOptions.ApiKey });
    builder.Services.AddScoped<IMutuelleCardOcrService, ClaudeMutuelleCardOcrService>();
    builder.Services.AddHostedService<MutuelleCardOcrDispatcher>();
}

// Signatures en lot (B5) : une requête pour tout le lot.
builder.Services.AddScoped<ISignatureQueryService, SignatureQueryService>();

// ── Ports ERP-backed (in-process) ───────────────────────────────────────────
builder.Services.AddScoped<ICrewRepository, CaSoft.Erp.USVector.Infrastructure.Repositories.Erp.CrewRepository>();
// MOB-6 : détail mission (mission + commande + bénéficiaire ERP → ClJob).
builder.Services.AddScoped<IJobRepository, CaSoft.Erp.USVector.Infrastructure.Repositories.Erp.JobRepository>();
// OC-3a : verrou + provenance du context de la mission, lus sur Orders.Api (lecture seule).
builder.Services.AddScoped<IContextOrderStateQueryService, CaSoft.Erp.USVector.Infrastructure.Repositories.Erp.ContextOrderStateQueryService>();
// Carte mutuelle par mission : le patient est résolu sur Orders.Api, le terrain ne connaissant que la mission.
builder.Services.AddScoped<IMissionBeneficiaryQueryService, CaSoft.Erp.USVector.Infrastructure.Repositories.Erp.MissionBeneficiaryQueryService>();
// OC-4 : relais de la sélection terrain vers Orders.Api. Branché sur POST api/Contract dès que le
// drapeau OC-3b est armé ; traduit l'id par code tant qu'il ne l'est pas.
builder.Services.AddScoped<IContextOrderSelectionService, CaSoft.Erp.USVector.Infrastructure.Repositories.Erp.ContextOrderSelectionService>();
// OC-3b : liste des types servie depuis le catalogue Order (déjà filtré agence/mode).
builder.Services.AddScoped<IContextOrderCatalogService, CaSoft.Erp.USVector.Infrastructure.Repositories.Erp.ContextOrderCatalogService>();
// OC-5 : formulaire d'attributs et valeurs saisies, servis et enregistrés par Order.
builder.Services.AddScoped<IContextOrderAttributeService, CaSoft.Erp.USVector.Infrastructure.Repositories.Erp.ContextOrderAttributeService>();
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

// ── Services applicatifs (portés tels quels de MobApp.Application) ───────────
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

// Mode formation : aiguillages devant les clients d'Orders et l'identité. APRÈS leur enregistrement.
builder.Services.AddVectorSimulationRouting(simulation);

// NLog
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// Orders indisponible → 503 ProblemDetails affichable, au lieu d'un 500 brut (voir le gestionnaire).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<OrdersUnavailableExceptionHandler>();

// CORS — TODO : restreindre (aligné sur le P0 #2 de l'ERP)
builder.Services.AddCors();

var app = builder.Build();

app.UseExceptionHandler();

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

if (keycloakLegacyKeys.Count > 0)
    app.Logger.LogWarning("Clés Keycloak à renommer (Itération 11), reprises sous leur nouveau nom : {Cles}.",
        string.Join(" ; ", keycloakLegacyKeys));

if (keycloakEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// DEC-6 · E1 — sonde de la surface anonyme (DEVPLAN_2 §6). Enregistrée APRÈS l'authentification :
// c'est ce qui permet de distinguer un appel nu d'un appel déjà porteur d'un jeton (azp). Lecture
// seule, aucune incidence sur la réponse ; journal dédié « Vector.SurfaceAnonyme » (nlog.config).
app.UseMiddleware<AnonymousSurfaceProbe>();

app.MapControllers();

app.Run();
