using CaSoft.Connectors.GpsGate;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.Persistence;
using CaSoft.Erp.USVector.Infrastructure.Repositories;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Mobile;
using CaSoft.Orders.Infrastructure;
using EmergencyPlatformConnector;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
            options.Authority = builder.Configuration["Keycloak:Authority"];
            options.Audience = builder.Configuration["Keycloak:Audience"];
            options.RequireHttpsMetadata = builder.Configuration.GetValue("Keycloak:RequireHttpsMetadata", true);
        });
    builder.Services.AddAuthorization();
}

// BD Mobile dédiée (MOB_* : sessions, timeline statuts, signatures)
builder.Services.AddDbContext<MobileDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MobileDb")));

// Accès données de référence ERP in-process (OrdersDbContext + query services).
// Lecture seule côté mobile : missions, équipages, véhicules, personnel, bénéficiaires.
builder.Services.AddOrdersInfrastructure(
    builder.Configuration.GetConnectionString("OrdersDb")
        ?? throw new InvalidOperationException("ConnectionStrings:OrdersDb manquant."),
    builder.Configuration["AddressApi:BaseUrl"]
        ?? throw new InvalidOperationException("AddressApi:BaseUrl manquant."));

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
});

// ── Ports BD Mobile → implémentations réelles (MOB-2) ────────────────────────
builder.Services.AddScoped<ISignatureRepository, SignatureRepository>();
builder.Services.AddScoped<IJobTimeRepository, JobTimeRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();

// ── Ports ERP-backed (in-process) ───────────────────────────────────────────
builder.Services.AddScoped<ICrewRepository, CaSoft.Erp.USVector.Infrastructure.Repositories.Erp.CrewRepository>();
// MOB-6 : détail mission (mission + commande + bénéficiaire ERP → ClJob).
builder.Services.AddScoped<IJobRepository, CaSoft.Erp.USVector.Infrastructure.Repositories.Erp.JobRepository>();
// MOB-4a : résolution identité Keycloak (sub → PER_ID → crews actifs).
builder.Services.AddScoped<IMobileIdentityResolver, CaSoft.Erp.USVector.Infrastructure.Repositories.Erp.MobileIdentityResolver>();

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
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "USVector API v2");
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
