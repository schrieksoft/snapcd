using MassTransit;
using MassTransit.SqlTransport;
using SnapCd.Server.Core.Consumers.System.Competing;
using SnapCd.Server.Core.Consumers.System.Fanout;
using SnapCd.Server.Core.Consumers.Tasks;
using SnapCd.Server.Core.Consumers.Tasks.Handlers;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Misc.Utils;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.StateMachine.Gatekeeping;
using SnapCd.Server.Core.StateMachine.Jobs;

namespace SnapCd.Server.Core.Startup;

public static class MassTransit
{
    public static IServiceCollection AddSnapCdMassTransitConfiguration(
        this IServiceCollection services,
        ConfigurationManager configuration,
        params Type[] additionalCompetingConsumerTypes)
    {
        var serviceBusSettings = configuration.GetSection("ServiceBus").Get<ServiceBusSettings>() ??
                                 throw new Exception("No 'ServiceBus' configuration section found. You must configure this in order for Snap CD Server to start up");

        // Read server instance ID from configuration
        var serverSettings = configuration.GetSection("Server").Get<ServerSettings>() ??
                             throw new Exception("No 'Server' configuration section found");
        var instanceId = serverSettings.InstanceId.ToString("N");

        if (serviceBusSettings.BusType == BusType.SqlServer)
        {
            var sqlOptions = serviceBusSettings.TransportOptions.SqlServer
                ?? new SqlServerTransportOptions();
            var sqlConnectionString = sqlOptions.ConnectionString
                ?? configuration["ConnectionString"]
                ?? throw new Exception("SqlServer bus requires either ServiceBus:TransportOptions:SqlServer:ConnectionString or the app's top-level ConnectionString");

            services.AddOptions<SqlTransportOptions>().Configure(o =>
            {
                o.ConnectionString = sqlConnectionString;
                o.Schema = sqlOptions.Schema;
            });
            services.AddSqlServerMigrationHostedService(create: true, delete: false);
        }

        services.AddMassTransit(x =>
        {
            AddSagaStateMachines(x);
            x.AddServerConsumers(instanceId, additionalCompetingConsumerTypes);

            switch (serviceBusSettings.BusType)
            {
                case BusType.AzureServiceBus:
                    if (serviceBusSettings.TransportOptions.AzureServiceBus == null)
                        throw new ApplicationException(
                            "Azure Service Bus selected as Bus Type, but its configuration is missing");
                    x.AddServiceBusMessageScheduler();

                    x.UsingAzureServiceBus((context, cfg) =>

                    {
                        var truncatedFormatter = new TruncatedKebabCaseEndpointNameFormatter(serviceBusSettings.EndpointsPrefix,
                            serviceBusSettings.EndpointsPrefixIncludeNameSpace, 255);

                        AddSagaReceiveEndpoints<IServiceBusBusFactoryConfigurator>(serviceBusSettings, context, cfg, truncatedFormatter);

                        var connectionString = serviceBusSettings.TransportOptions.AzureServiceBus.ConnectionString;

                        // Check if it's a URI format (sb://) or a connection string format (Endpoint=)
                        if (connectionString.StartsWith("sb://", StringComparison.OrdinalIgnoreCase))
                            cfg.Host(new Uri(connectionString));
                        else
                            cfg.Host(connectionString);
                        // Configure runner consumer endpoints with auto-delete settings
                        foreach (var consumerType in RunnerConsumerTypes)
                        {
                            var queueName = $"runner--{instanceId}--{GetMessageName(consumerType).ToLower()}";
                            cfg.ReceiveEndpoint(queueName, e =>
                            {
                                e.AutoDeleteOnIdle = TimeSpan.FromMinutes(5);
                                e.DefaultMessageTimeToLive = TimeSpan.FromMinutes(10); // needed since otherwise AutoDeleteOnIdle might never trigger
                                e.ConfigureConsumer(context, consumerType);
                            });
                        }

                        // Configure fanout consumer endpoints with auto-delete settings
                        foreach (var consumerType in ServerFanoutConsumerTypes)
                        {
                            var queueName = $"fanout--{instanceId}--{GetMessageName(consumerType).ToLower()}";
                            cfg.ReceiveEndpoint(queueName, e =>
                            {
                                e.AutoDeleteOnIdle = TimeSpan.FromMinutes(5);
                                e.DefaultMessageTimeToLive = TimeSpan.FromMinutes(10); // needed since otherwise AutoDeleteOnIdle might never trigger
                                e.ConfigureConsumer(context, consumerType);
                            });
                        }

                        cfg.ConfigureEndpoints(context, truncatedFormatter);

                        cfg.UseServiceBusMessageScheduler();
                    });
                    break;

                case BusType.SqlServer:
                    x.AddSqlMessageScheduler();

                    x.UsingSqlServer((context, cfg) =>
                    {
                        var truncatedFormatter = new TruncatedKebabCaseEndpointNameFormatter(
                            serviceBusSettings.EndpointsPrefix,
                            serviceBusSettings.EndpointsPrefixIncludeNameSpace, 255);

                        AddSagaReceiveEndpoints<ISqlBusFactoryConfigurator>(serviceBusSettings, context, cfg, truncatedFormatter);

                        // Runner consumer endpoints — instance-specific; auto-expire mirrors the ASB config.
                        foreach (var consumerType in RunnerConsumerTypes)
                        {
                            var queueName = $"runner--{instanceId}--{GetMessageName(consumerType).ToLower()}";
                            cfg.ReceiveEndpoint(queueName, e =>
                            {
                                e.AutoDeleteOnIdle = TimeSpan.FromMinutes(5);
                                e.ConfigureConsumer(context, consumerType);
                            });
                        }

                        foreach (var consumerType in ServerFanoutConsumerTypes)
                        {
                            var queueName = $"fanout--{instanceId}--{GetMessageName(consumerType).ToLower()}";
                            cfg.ReceiveEndpoint(queueName, e =>
                            {
                                e.AutoDeleteOnIdle = TimeSpan.FromMinutes(5);
                                e.ConfigureConsumer(context, consumerType);
                            });
                        }

                        cfg.ConfigureEndpoints(context, truncatedFormatter);

                        cfg.UseSqlMessageScheduler();
                    });
                    break;
            }
        });

        return services;
    }

    // Runner consumers - need instance-specific endpoints for targeted sends
    private static readonly Type[] RunnerConsumerTypes =
    [
        typeof(GetDefinitiveRevisionConsumer),
        typeof(GetModuleConsumer),
        typeof(InitConsumer),
        typeof(ValidateConsumer),
        typeof(VariablesConsumer),
        typeof(PlanConsumer),
        typeof(PlanDestroyConsumer),
        typeof(ApplyFromPlanConsumer),
        typeof(DestroyFromPlanConsumer),
        typeof(OutputConsumer),

        // cancel
        typeof(CancelKillConsumer),
        typeof(CancelGracefulConsumer),

        // heartbeat
        typeof(HeartbeatConsumer)
    ];

    private static readonly Type[] ServerCompetingConsumerTypes =
    [
        // System consumers
        typeof(SelectRunnerInstanceConsumer),
        typeof(OutputSetWithOutputsCreatedCompetingConsumer),
        typeof(ModuleModifiedCompetingConsumer),
        typeof(NamespaceModifiedCompetingConsumer),
        typeof(StackModifiedCompetingConsumer),
        typeof(SourceRefreshCompletedCompetingConsumer),
        typeof(NamespaceApprovalThresholdModifiedCompetingConsumer),
        typeof(ModuleApprovalThresholdModifiedCompetingConsumer),
        typeof(ModuleJobApprovalModifiedCompetingConsumer),
        typeof(SecretModifiedCompetingConsumer),
        typeof(ModuleStateChangedToAppliedCompetingConsumer),
        typeof(ModuleStateChangedToDestroyedCompetingConsumer),

        // Handler consumers (offloaded from SignalR handlers)
        typeof(OutputCompletedInvokedConsumer),
        typeof(VariablesCompletedInvokedConsumer),
        typeof(ReportRunningTaskInvokedConsumer),

        // Admin notification consumers

        // Cache invalidation consumers
        typeof(OrganizationMembershipCacheInvalidationConsumer),
    ];

    private static readonly Type[] ServerFanoutConsumerTypes =
    [
        typeof(JobCreatedFanoutConsumer),
        typeof(JobUpdatedFanoutConsumer),
        typeof(LogReceivedFanoutConsumer),
        typeof(RunnerAvailabilityModifiedFanoutConsumer),
        typeof(ModuleSagaModifiedFanoutConsumer),
        typeof(ModuleStateModifiedFanoutConsumer),
        typeof(ModuleJobApprovalModifiedFanoutConsumer),
        typeof(ModuleApprovalThresholdModifiedFanoutConsumer),
        typeof(ModuleResourceCountUpdatedFanoutConsumer),
        typeof(ServerHeartbeatConsumer)
    ];


    private static void AddSagaReceiveEndpoints<TMqFactory>(ServiceBusSettings serviceBusSettings, IBusRegistrationContext context,
        TMqFactory cfg, IEndpointNameFormatter? endpointNameFormatter = null) where TMqFactory : IBusFactoryConfigurator
    {
        AddSagaReceiveEndpoint<ApplyJobSaga, TMqFactory>(serviceBusSettings, context, cfg, endpointNameFormatter);
        AddSagaReceiveEndpoint<DestroyJobSaga, TMqFactory>(serviceBusSettings, context, cfg, endpointNameFormatter);

        AddSagaReceiveEndpoint<ModuleSaga, TMqFactory>(serviceBusSettings, context, cfg, serviceBusSettings.SagaConcurrencyLimit, s =>
        {
            var partition = s.CreatePartitioner(1);
            // Concurrency limit of 1 for GatekeepingJobRequested (multiple events c can never be processed in parallel)
            s.Message<GatekeepingJobRequested>(x => x.UsePartitioner(partition, m => m.Message.ModuleId));

            // Concurrency limit of 1 for ModuleDependencyCheckRequested (multiple events can never be processed in parallel)
            s.Message<ModuleDependencyCheckRequested>(x => x.UsePartitioner(partition, m => m.Message.ModuleId));

            // Concurrency limit of 1 for DriftCheckScheduled (partition by ModuleId)
            s.Message<DriftCheckScheduled>(x => x.UsePartitioner(partition, m => m.Message.ModuleId));
        }, endpointNameFormatter);

        // ModuleModifiedSaga with concurrency limit of 1 for ModuleModifiedTriggerRequested
        AddSagaReceiveEndpoint<ModuleModifiedSaga, TMqFactory>(serviceBusSettings, context, cfg, serviceBusSettings.SagaConcurrencyLimit, s =>
        {
            var partition = s.CreatePartitioner(1);
            s.Message<ModuleModifiedTriggerRequested>(x => x.UsePartitioner(partition, m => m.Message.ModuleId));
        }, endpointNameFormatter);
    }

    private static void AddSagaStateMachines(IBusRegistrationConfigurator x)
    {
        AddSagaStateMachine<ModuleStateMachine, ModuleSaga>(x);
        AddSagaStateMachine<ModuleModifiedStateMachine, ModuleModifiedSaga>(x);

        // module sagas
        AddSagaStateMachine<
            JobStateMachine<
                ApplyJobSaga,
                ApplyModuleRequested,
                ApplyModuleFailed,
                ApplyModuleCompleted,
                ApplyModuleCancelled,
                PlanRequested,
                PlanCompleted,
                PlanCancelled,
                ApplyFromPlanRequested,
                ApplyFromPlanCompleted,
                ApplyFromPlanCancelled
            >,
            ApplyJobSaga>(x);

        AddSagaStateMachine<
            JobStateMachine<
                DestroyJobSaga,
                DestroyModuleRequested,
                DestroyModuleFailed,
                DestroyModuleCompleted,
                DestroyModuleCancelled,
                PlanDestroyRequested,
                PlanDestroyCompleted,
                PlanDestroyCancelled,
                DestroyFromPlanRequested,
                DestroyFromPlanCompleted,
                DestroyFromPlanCancelled
            >, DestroyJobSaga>(x);
    }


    private static void AddServerConsumers(this IBusRegistrationConfigurator configurator, string instanceId, Type[] additionalCompetingConsumerTypes)
    {
        // Runner consumers - endpoints configured manually per transport
        foreach (var consumerType in RunnerConsumerTypes)
            configurator.AddConsumer(consumerType);

        // System competing consumers share queues across all servers
        foreach (var consumerType in ServerCompetingConsumerTypes)
            configurator.AddConsumer(consumerType);

        // Additional competing consumers registered by the hosting application (e.g. SaaS).
        foreach (var consumerType in additionalCompetingConsumerTypes)
            configurator.AddConsumer(consumerType);

        // Fanout consumers - endpoints configured manually per transport
        foreach (var consumerType in ServerFanoutConsumerTypes)
            configurator.AddConsumer(consumerType);
    }

    /// <summary>
    /// Extracts the message type name from a consumer type that implements IConsumer&lt;TMessage&gt;
    /// Returns only the simple type name without namespace (e.g., "GetModuleRequested" not "SnapCd.Server.Events.Steps.GetModuleRequested")
    /// </summary>
    private static string GetMessageName(Type consumerType)
    {
        // Find IConsumer<TMessage> interface
        var consumerInterface = consumerType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>));

        if (consumerInterface == null)
            throw new InvalidOperationException($"Consumer type {consumerType.Name} does not implement IConsumer<>");

        // Get the message type (TMessage) and return just the name without namespace
        var messageType = consumerInterface.GetGenericArguments()[0];
        var typeName = messageType.Name;

        // Type.Name should already exclude namespace, but ensure no namespace prefix exists
        var lastDotIndex = typeName.LastIndexOf('.');
        return lastDotIndex >= 0 ? typeName.Substring(lastDotIndex + 1) : typeName;
    }

    private static void AddSagaReceiveEndpoint<TSaga, TMqFactory>(ServiceBusSettings serviceBusSettings, IBusRegistrationContext context, TMqFactory cfg,
        IEndpointNameFormatter? endpointNameFormatter = null)
        where TSaga : class, SagaStateMachineInstance
        where TMqFactory : IBusFactoryConfigurator
    {
        var formatter = endpointNameFormatter ?? new KebabCaseEndpointNameFormatter(serviceBusSettings.EndpointsPrefix, serviceBusSettings.EndpointsPrefixIncludeNameSpace);
        var endpointName = formatter.Saga<TSaga>();

        cfg.ReceiveEndpoint(endpointName, e =>
        {
            e.PrefetchCount = serviceBusSettings.SagaConcurrencyLimit;
            e.UseMessageRetry(r => r.Interval(5, 1000));
            e.UseMessageScope(context);
            e.UseInMemoryOutbox(context);
            e.ConfigureSaga<TSaga>(context);
        });
    }

    private static void AddSagaReceiveEndpoint<TSaga, TMqFactory>(ServiceBusSettings serviceBusSettings, IBusRegistrationContext context, TMqFactory cfg,
        int concurrencyLimit, Action<ISagaConfigurator<TSaga>>? sagaConfigurator = null,
        IEndpointNameFormatter? endpointNameFormatter = null)
        where TSaga : class, SagaStateMachineInstance
        where TMqFactory : IBusFactoryConfigurator
    {
        var formatter = endpointNameFormatter ?? new KebabCaseEndpointNameFormatter(serviceBusSettings.EndpointsPrefix, serviceBusSettings.EndpointsPrefixIncludeNameSpace);
        var endpointName = formatter.Saga<TSaga>();

        cfg.ReceiveEndpoint(endpointName, e =>
        {
            e.PrefetchCount = concurrencyLimit;
            e.UseMessageRetry(r => r.Interval(5, 1000));
            e.UseMessageScope(context);
            e.UseInMemoryOutbox(context);

            e.ConfigureSaga<TSaga>(context, s => { sagaConfigurator?.Invoke(s); });
        });
    }


    private static void AddSagaStateMachine<TStateMachine, TSaga>(
        IBusRegistrationConfigurator configurator)
        where TStateMachine : class, SagaStateMachine<TSaga>
        where TSaga : class, SagaStateMachineInstance
    {
        configurator.AddSagaStateMachine<TStateMachine, TSaga>()
            .EntityFrameworkRepository(r =>
            {
                r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                r.ExistingDbContext<SnapCdDbContext>();
            });
    }
}