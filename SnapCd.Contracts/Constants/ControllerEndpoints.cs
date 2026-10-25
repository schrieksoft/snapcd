// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Constants;

public static class ControllerEndpoints
{
    public const string MissionRun = "api/{organizationId:guid}/MissionRun";
    public const string Runner = "api/{organizationId:guid}/Runner";
    public const string Namespace = "api/{organizationId:guid}/Namespace";
    public const string NamespaceInputFromLiteral = "api/{organizationId:guid}/NamespaceInputFromLiteral";
    public const string NamespaceInputFromSecret = "api/{organizationId:guid}/NamespaceInputFromSecret";
    public const string NamespaceInputFromDefinition = "api/{organizationId:guid}/NamespaceInputFromDefinition";
    public const string ModuleInputFromDefinition = "api/{organizationId:guid}/ModuleInputFromDefinition";
    public const string ModuleInputFromNamespace = "api/{organizationId:guid}/ModuleInputFromNamespace";
    public const string ModuleInputFromLiteral = "api/{organizationId:guid}/ModuleInputFromLiteral";
    public const string ModuleInputFromOutputSet = "api/{organizationId:guid}/ModuleInputFromOutputSet";
    public const string ModuleInputFromOutput = "api/{organizationId:guid}/ModuleInputFromOutput";
    public const string ModuleInputFromSecret = "api/{organizationId:guid}/ModuleInputFromSecret";
    public const string Stack = "api/{organizationId:guid}/Stack";
    public const string Module = "api/{organizationId:guid}/Module";
    public const string ModuleHold = "api/{organizationId:guid}/ModuleHold";
    public const string Transfer = "api/{organizationId:guid}/Transfer";
    public const string Integration = "api/{organizationId:guid}/Integration";
    public const string OrganizationIntegrationEvent = "api/{organizationId:guid}/OrganizationIntegrationEvent";
    public const string StackIntegrationEvent = "api/{organizationId:guid}/StackIntegrationEvent";
    public const string NamespaceIntegrationEvent = "api/{organizationId:guid}/NamespaceIntegrationEvent";
    public const string ModuleIntegrationEvent = "api/{organizationId:guid}/ModuleIntegrationEvent";
    public const string ServicePrincipal = "api/{organizationId:guid}/ServicePrincipal";
    public const string User = "api/{organizationId:guid}/User";
    public const string Group = "api/{organizationId:guid}/Group";
    public const string GroupMember = "api/{organizationId:guid}/GroupMember";
    public const string OrganizationRoleAssignment = "api/{organizationId:guid}/OrganizationRoleAssignment";
    public const string StackRoleAssignment = "api/{organizationId:guid}/StackRoleAssignment";
    public const string NamespaceRoleAssignment = "api/{organizationId:guid}/NamespaceRoleAssignment";
    public const string ModuleRoleAssignment = "api/{organizationId:guid}/ModuleRoleAssignment";
    public const string RunnerRoleAssignment = "api/{organizationId:guid}/RunnerRoleAssignment";
    public const string StackSecret = "api/{organizationId:guid}/StackSecret";
    public const string NamespaceSecret = "api/{organizationId:guid}/NamespaceSecret";
    public const string ModuleSecret = "api/{organizationId:guid}/ModuleSecret";
    public const string RunnerStackSupply = "api/{organizationId:guid}/RunnerStackSupply";
    public const string RunnerNamespaceSupply = "api/{organizationId:guid}/RunnerNamespaceSupply";
    public const string RunnerModuleSupply = "api/{organizationId:guid}/RunnerModuleSupply";
    public const string AgentStackSupply = "api/{organizationId:guid}/AgentStackSupply";
    public const string AgentNamespaceSupply = "api/{organizationId:guid}/AgentNamespaceSupply";
    public const string AgentModuleSupply = "api/{organizationId:guid}/AgentModuleSupply";
    public const string ModuleExtraFile = "api/{organizationId:guid}/ModuleExtraFile";
    public const string ModuleAdditionalTriggerPath = "api/{organizationId:guid}/ModuleAdditionalTriggerPath";
    public const string NamespaceAdditionalTriggerPath = "api/{organizationId:guid}/NamespaceAdditionalTriggerPath";
    public const string ModuleTerraformInlinePolicy = "api/{organizationId:guid}/ModuleTerraformInlinePolicy";
    public const string ModuleTerraformRemotePolicy = "api/{organizationId:guid}/ModuleTerraformRemotePolicy";
    public const string ModuleTerraformLocalPolicy = "api/{organizationId:guid}/ModuleTerraformLocalPolicy";
    public const string ModulePulumiInlinePolicy = "api/{organizationId:guid}/ModulePulumiInlinePolicy";
    public const string ModulePulumiRemotePolicy = "api/{organizationId:guid}/ModulePulumiRemotePolicy";
    public const string ModulePulumiLocalPolicy = "api/{organizationId:guid}/ModulePulumiLocalPolicy";
    public const string NamespaceTerraformInlinePolicy = "api/{organizationId:guid}/NamespaceTerraformInlinePolicy";
    public const string NamespaceTerraformRemotePolicy = "api/{organizationId:guid}/NamespaceTerraformRemotePolicy";
    public const string NamespaceTerraformLocalPolicy = "api/{organizationId:guid}/NamespaceTerraformLocalPolicy";
    public const string NamespacePulumiInlinePolicy = "api/{organizationId:guid}/NamespacePulumiInlinePolicy";
    public const string NamespacePulumiRemotePolicy = "api/{organizationId:guid}/NamespacePulumiRemotePolicy";
    public const string NamespacePulumiLocalPolicy = "api/{organizationId:guid}/NamespacePulumiLocalPolicy";
    public const string DependsOnModule = "api/{organizationId:guid}/DependsOnModule";
    public const string NamespaceExtraFile = "api/{organizationId:guid}/NamespaceExtraFile";
    public const string NamespaceBackendConfig = "api/{organizationId:guid}/NamespaceBackendConfig";
    public const string ModuleBackendConfig = "api/{organizationId:guid}/ModuleBackendConfig";
    public const string ModulePulumiFlag = "api/{organizationId:guid}/ModulePulumiFlag";
    public const string ModulePulumiArrayFlag = "api/{organizationId:guid}/ModulePulumiArrayFlag";
    public const string NamespacePulumiFlag = "api/{organizationId:guid}/NamespacePulumiFlag";
    public const string NamespacePulumiArrayFlag = "api/{organizationId:guid}/NamespacePulumiArrayFlag";
    public const string NamespaceTerraformFlag = "api/{organizationId:guid}/NamespaceTerraformFlag";
    public const string NamespaceTerraformArrayFlag = "api/{organizationId:guid}/NamespaceTerraformArrayFlag";
    public const string ModuleTerraformFlag = "api/{organizationId:guid}/ModuleTerraformFlag";
    public const string ModuleTerraformArrayFlag = "api/{organizationId:guid}/ModuleTerraformArrayFlag";
    public const string ModuleHook = "api/{organizationId:guid}/ModuleHook";
    public const string NamespaceHook = "api/{organizationId:guid}/NamespaceHook";
    public const string SourceChangedNotification = "api/{organizationId:guid}/SourceChangedNotification";
    public const string Jobs = "api/{organizationId:guid}/Job";
    public const string SourceRefresherPreselection = "api/{organizationId:guid}/SourceRefresherPreselection";
    public const string Agent = "api/{organizationId:guid}/Agent";
    public const string OrganizationMission = "api/{organizationId:guid}/OrganizationMission";
    public const string StackMission = "api/{organizationId:guid}/StackMission";
    public const string NamespaceMission = "api/{organizationId:guid}/NamespaceMission";
    public const string ModuleMission = "api/{organizationId:guid}/ModuleMission";
    public const string AgentRoleAssignment = "api/{organizationId:guid}/AgentRoleAssignment";
    public const string IntegrationRoleAssignment = "api/{organizationId:guid}/IntegrationRoleAssignment";
    public const string IntegrationStackSupply = "api/{organizationId:guid}/IntegrationStackSupply";
    public const string IntegrationNamespaceSupply = "api/{organizationId:guid}/IntegrationNamespaceSupply";
    public const string IntegrationModuleSupply = "api/{organizationId:guid}/IntegrationModuleSupply";
    public const string StateStore = "api/{organizationId:guid}/StateStore";
    public const string StateFile = "api/{organizationId:guid}/StateFile";
    public const string StateStoreRoleAssignment = "api/{organizationId:guid}/StateStoreRoleAssignment";
    public const string UserFavorite = "api/{organizationId:guid}/UserFavorite";
    public const string UserColor = "api/{organizationId:guid}/UserColor";
}