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
    public const string MissionRun = "api/{organizationId}/MissionRun";
    public const string Runner = "api/{organizationId}/Runner";
    public const string Namespace = "api/{organizationId}/Namespace";
    public const string NamespaceInputFromLiteral = "api/{organizationId}/NamespaceInputFromLiteral";
    public const string NamespaceInputFromSecret = "api/{organizationId}/NamespaceInputFromSecret";
    public const string NamespaceInputFromDefinition = "api/{organizationId}/NamespaceInputFromDefinition";
    public const string ModuleInputFromDefinition = "api/{organizationId}/ModuleInputFromDefinition";
    public const string ModuleInputFromNamespace = "api/{organizationId}/ModuleInputFromNamespace";
    public const string ModuleInputFromLiteral = "api/{organizationId}/ModuleInputFromLiteral";
    public const string ModuleInputFromOutputSet = "api/{organizationId}/ModuleInputFromOutputSet";
    public const string ModuleInputFromOutput = "api/{organizationId}/ModuleInputFromOutput";
    public const string ModuleInputFromSecret = "api/{organizationId}/ModuleInputFromSecret";
    public const string Stack = "api/{organizationId}/Stack";
    public const string Module = "api/{organizationId}/Module";
    public const string Integration = "api/{organizationId}/Integration";
    public const string IntegrationEvent = "api/{organizationId}/IntegrationEvent";
    public const string OrganizationIntegrationEvent = "api/{organizationId}/OrganizationIntegrationEvent";
    public const string StackIntegrationEvent = "api/{organizationId}/StackIntegrationEvent";
    public const string NamespaceIntegrationEvent = "api/{organizationId}/NamespaceIntegrationEvent";
    public const string ModuleIntegrationEvent = "api/{organizationId}/ModuleIntegrationEvent";
    public const string Logs = "api/{organizationId}/Logs";
    public const string ServicePrincipal = "api/{organizationId}/ServicePrincipal";
    public const string User = "api/{organizationId}/User";
    public const string Group = "api/{organizationId}/Group";
    public const string GroupMember = "api/{organizationId}/GroupMember";
    public const string OrganizationRoleAssignment = "api/{organizationId}/OrganizationRoleAssignment";
    public const string StackRoleAssignment = "api/{organizationId}/StackRoleAssignment";
    public const string NamespaceRoleAssignment = "api/{organizationId}/NamespaceRoleAssignment";
    public const string ModuleRoleAssignment = "api/{organizationId}/ModuleRoleAssignment";
    public const string RunnerRoleAssignment = "api/{organizationId}/RunnerRoleAssignment";
    public const string StackSecret = "api/{organizationId}/StackSecret";
    public const string NamespaceSecret = "api/{organizationId}/NamespaceSecret";
    public const string ModuleSecret = "api/{organizationId}/ModuleSecret";
    public const string RunnerStackSupply = "api/{organizationId}/RunnerStackSupply";
    public const string RunnerNamespaceSupply = "api/{organizationId}/RunnerNamespaceSupply";
    public const string RunnerModuleSupply = "api/{organizationId}/RunnerModuleSupply";
    public const string AgentStackSupply = "api/{organizationId}/AgentStackSupply";
    public const string AgentNamespaceSupply = "api/{organizationId}/AgentNamespaceSupply";
    public const string AgentModuleSupply = "api/{organizationId}/AgentModuleSupply";
    public const string ModuleExtraFile = "api/{organizationId}/ModuleExtraFile";
    public const string DependsOnModule = "api/{organizationId}/DependsOnModule";
    public const string NamespaceExtraFile = "api/{organizationId}/NamespaceExtraFile";
    public const string NamespaceBackendConfig = "api/{organizationId}/NamespaceBackendConfig";
    public const string ModuleBackendConfig = "api/{organizationId}/ModuleBackendConfig";
    public const string ModulePulumiFlag = "api/{organizationId}/ModulePulumiFlag";
    public const string ModulePulumiArrayFlag = "api/{organizationId}/ModulePulumiArrayFlag";
    public const string NamespacePulumiFlag = "api/{organizationId}/NamespacePulumiFlag";
    public const string NamespacePulumiArrayFlag = "api/{organizationId}/NamespacePulumiArrayFlag";
    public const string NamespaceTerraformFlag = "api/{organizationId}/NamespaceTerraformFlag";
    public const string NamespaceTerraformArrayFlag = "api/{organizationId}/NamespaceTerraformArrayFlag";
    public const string ModuleTerraformFlag = "api/{organizationId}/ModuleTerraformFlag";
    public const string ModuleTerraformArrayFlag = "api/{organizationId}/ModuleTerraformArrayFlag";
    public const string ModuleHook = "api/{organizationId}/ModuleHook";
    public const string NamespaceHook = "api/{organizationId}/NamespaceHook";
    public const string SourceChangedNotification = "api/{organizationId}/SourceChangedNotification";
    public const string Jobs = "api/{organizationId}/Job";
    public const string SourceRefresherPreselection = "api/{organizationId}/SourceRefresherPreselection";
    public const string Agent = "api/{organizationId}/Agent";
    public const string OrganizationMission = "api/{organizationId}/OrganizationMission";
    public const string StackMission = "api/{organizationId}/StackMission";
    public const string NamespaceMission = "api/{organizationId}/NamespaceMission";
    public const string ModuleMission = "api/{organizationId}/ModuleMission";
    public const string AgentRoleAssignment = "api/{organizationId}/AgentRoleAssignment";
    public const string IntegrationRoleAssignment = "api/{organizationId}/IntegrationRoleAssignment";
    public const string IntegrationStackSupply = "api/{organizationId}/IntegrationStackSupply";
    public const string IntegrationNamespaceSupply = "api/{organizationId}/IntegrationNamespaceSupply";
    public const string IntegrationModuleSupply = "api/{organizationId}/IntegrationModuleSupply";
}