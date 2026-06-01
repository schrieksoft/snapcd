// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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

