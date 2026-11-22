using CaSoft.Erp.USVector.Api.Workers;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Simulation;

namespace CaSoft.Erp.USVector.Api.Infrastructure;

/// <summary>Enregistrement du mode formation dans la Composition Root.</summary>
public static class SimulationRegistration
{
    /// <summary>
    /// Lit la section <c>Simulation</c>, refuse les personas en Production, et pose ce qui sert même mode coupé
    /// (le filtre doit pouvoir refuser l'en-tête de formation d'une app sur un serveur qui ne l'ouvre pas).
    /// </summary>
    public static SimulationOptions AddVectorSimulationOptions(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        var options = configuration.GetSection(SimulationOptions.SectionName).Get<SimulationOptions>() ?? new SimulationOptions();
        if (options.AnonymousPersonas && environment.IsProduction())
            throw new InvalidOperationException(
                "Simulation:AnonymousPersonas est interdit en Production : il ouvre des comptes sans Keycloak. "
                + "Le mode formation des équipages n'en a pas besoin (Simulation:Enabled suffit).");

        services.AddSingleton(options);
        services.AddSingleton<ClSimulationState>();
        services.AddScoped<ClSimulationMobileStore>();
        services.AddScoped<SimulationConfinementFilter>();
        return options;
    }

    /// <summary>
    /// Pose les aiguillages devant les clients d'Orders et le résolveur d'identité. À appeler APRÈS leur
    /// enregistrement : chaque aiguillage enveloppe l'enregistrement existant. Sans effet si le mode est coupé
    /// — le chemin réel reste alors exactement celui d'avant.
    /// </summary>
    public static void AddVectorSimulationRouting(this IServiceCollection services, SimulationOptions options)
    {
        if (!options.Enabled) return;

        services.AddHttpContextAccessor();
        Decorate<IErpReadApiClient>(services, (sp, real) =>
            new ClSimulationRoutingErpReadApiClient(real, new ClSimulatedOrdersReadClient(sp.GetRequiredService<ClSimulationState>())));
        Decorate<IErpWriteApiClient>(services, (sp, real) =>
            new ClSimulationRoutingErpWriteApiClient(real, new ClSimulatedOrdersWriteClient(sp.GetRequiredService<ClSimulationState>())));
        Decorate<IShiftConfirmationService>(services, (_, real) => new ClSimulationRoutingShiftConfirmationService(real));
        Decorate<IBeneficiaryFieldEntryService>(services, (_, real) => new ClSimulationRoutingBeneficiaryFieldEntryService(real));
        Decorate<IMobileIdentityResolver>(services, (sp, real) =>
        {
            var http = sp.GetRequiredService<IHttpContextAccessor>();
            return new ClSimulationIdentityResolver(real, () => SimulationRequest.IsTrainingRequested(http.HttpContext, options));
        });
        services.AddHostedService<SimulationPurgeWorker>();
    }

    /// <summary>Remplace l'enregistrement de <typeparamref name="T"/> par un décorateur, même durée de vie.</summary>
    private static void Decorate<T>(IServiceCollection services, Func<IServiceProvider, T, T> decorate) where T : class
    {
        var original = services.LastOrDefault(d => d.ServiceType == typeof(T))
            ?? throw new InvalidOperationException($"{typeof(T).Name} doit être enregistré avant le mode formation.");
        services.Remove(original);
        services.Add(new ServiceDescriptor(typeof(T), sp => decorate(sp, (T)Resolve(sp, original)), original.Lifetime));
    }

    private static object Resolve(IServiceProvider sp, ServiceDescriptor descriptor)
        => descriptor.ImplementationInstance
           ?? descriptor.ImplementationFactory?.Invoke(sp)
           ?? ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType!);
}
