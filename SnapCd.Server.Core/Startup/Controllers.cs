// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Controllers;
using SnapCd.Server.Core.Controllers;
using SnapCd.Server.Core.Controllers.Crud;
using SnapCd.Server.Core.Controllers.Crud.Secrets;
using SnapCd.Server.Core.Controllers.Hooks;
using SnapCd.Server.Core.Controllers.Jobs;
using SnapCd.Server.Core.Controllers.Logs;
using SnapCd.Server.Core.Controllers.OpenIddict;

namespace SnapCd.Server.Core.Startup;

public static class Controllers
{
    public static IServiceCollection AddSnapCdControllers(this IServiceCollection services, IEnumerable<Type>? additionalControllers = null)
    {
        var controllers = new List<Type>();
        controllers.AddRange(new[]
        {
            typeof(MissionRunController),
            typeof(StackController),
            typeof(NamespaceController),
            typeof(ModuleController),
            typeof(ModuleInputFromDefinitionController),
            typeof(ModuleInputFromLiteralController),
            typeof(ModuleInputFromNamespaceController),
            typeof(ModuleInputFromOutputController),
            typeof(ModuleInputFromOutputSetController),
            typeof(ModuleInputFromSecretController),
            typeof(NamespaceInputFromLiteralController),
            typeof(NamespaceInputFromDefinitionController),
            typeof(NamespaceInputFromSecretController),
            typeof(LogsController),
            typeof(RunnerController),
            typeof(AgentController),
            typeof(ServicePrincipalController),
            typeof(GroupController),
            typeof(GroupMemberController),
            typeof(OrganizationUserController),
            typeof(OrganizationRoleAssignmentController),
            typeof(StackRoleAssignmentController),
            typeof(NamespaceRoleAssignmentController),
            typeof(ModuleRoleAssignmentController),
            typeof(RunnerRoleAssignmentController),
            typeof(AgentRoleAssignmentController),
            typeof(RunnerStackAssignmentController),
            typeof(RunnerNamespaceAssignmentController),
            typeof(RunnerModuleAssignmentController),
            typeof(AgentStackAssignmentController),
            typeof(AgentNamespaceAssignmentController),
            typeof(AgentModuleAssignmentController),
            typeof(OrganizationMissionController),
            typeof(StackMissionController),
            typeof(NamespaceMissionController),
            typeof(ModuleMissionController),
            typeof(SourceRefresherPreselectionController),
            typeof(NamespaceExtraFileController),
            typeof(ModuleExtraFileController),
            typeof(DependsOnModuleController),
            typeof(NamespacePulumiFlagController),
            typeof(NamespacePulumiArrayFlagController),
            typeof(ModulePulumiFlagController),
            typeof(ModulePulumiArrayFlagController),
            typeof(NamespaceTerraformFlagController),
            typeof(NamespaceTerraformArrayFlagController),
            typeof(ModuleTerraformFlagController),
            typeof(ModuleTerraformArrayFlagController),
            typeof(ModuleHookController),
            typeof(NamespaceHookController),
            typeof(SourceChangedNotificationController),

            typeof(OrganizationSwitchController),
            typeof(ModuleSecretController),
            typeof(NamespaceSecretController),
            typeof(StackSecretController),

            // OpenIddict
            typeof(AuthenticationController),
            typeof(AuthorizationController),
            typeof(ErrorController),
            typeof(HomeController),
            typeof(ResourceController),
            typeof(UserInfoController)

        });

        if (additionalControllers != null)
            controllers.AddRange(additionalControllers);


        services.AddControllers(options =>
            {
            })
            .ConfigureApplicationPartManager(manager =>
            {
                // Get the default controller feature provider
                var controllerFeatureProvider =
                    manager.FeatureProviders.OfType<ControllerFeatureProvider>().FirstOrDefault();
                if (controllerFeatureProvider != null)
                {
                    // Remove the default feature provider (which would bind all controllers in the assembly)
                    manager.FeatureProviders.Remove(controllerFeatureProvider);

                    // Bind only our defined list of controllers
                    manager.FeatureProviders.Add(new CustomControllerFeatureProvider(controllers));
                }
            })
            .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

        services.AddControllersWithViews(); // This is needed for the Authorization and Error controllers, both of which use Views.

        return services;
    }
}