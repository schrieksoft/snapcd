// using MassTransit;
// using Microsoft.Extensions.Options;
// using SnapCd.Server.Clients;
// using SnapCd.Server.Clients.ModuleCache;
// using SnapCd.Server.Events.Processing;
// using SnapCd.Server.Services;
// using SnapCd.Server.Settings.Runner;
//
// namespace SnapCd.Server.Factories;
//
// public class LegacyTerraformModuleManagerFactory
// {
//     private readonly IModuleCacheClient _moduleCacheClient;
//     private readonly IOptions<ModuleManagerSettings> _options;
//     private readonly ILoggerFactory _loggerFactory;
//
//
//     public LegacyTerraformModuleManagerFactory(
//         IOptions<ModuleManagerSettings> options,
//         IModuleCacheClient moduleCacheClient,
//         ILoggerFactory loggerFactory
//     )
//     {
//         _options = options;
//         _moduleCacheClient = moduleCacheClient;
//         _loggerFactory = loggerFactory;
//     }
//
//
//     public LegacyTerraformModuleManager<TRequest> Create<TRequest>(Git<TRequest> git, ConsumeContext<TRequest> context)
//         where TRequest : ProcessingModuleRequestBase
//     {
//         var logger = _loggerFactory.CreateLogger<LegacyTerraformModuleManager<TRequest>>();
//
//         return new LegacyTerraformModuleManager<TRequest>(
//             logger,
//             _options,
//             _moduleCacheClient,
//             git,
//             context
//         );
//     }
// }

