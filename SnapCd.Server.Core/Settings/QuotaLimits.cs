// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings;

public class QuotaLimits
{
    // Default quota for any unspecified field
    public int? DefaultQuota { get; set; }

    // Primary entities
    public int? StackQuota { get; set; }
    public int? NamespaceQuota { get; set; }
    public int? ModuleQuota { get; set; }
    public int? RunnerQuota { get; set; }
    public int? GroupQuota { get; set; }
    public int? ServicePrincipalQuota { get; set; }
    public int? OrganizationUserQuota { get; set; }
    public int? SecretQuota { get; set; }
    public int? VariableSetQuota { get; set; }
    public int? OutputSetQuota { get; set; }

    // Output and Variable quotas (org-level totals)
    public int? OutputQuota { get; set; }
    public int? VariableQuota { get; set; }

    // Per-set quotas (max items per individual set)
    public int? OutputPerSetQuota { get; set; }
    public int? VariablePerSetQuota { get; set; }

    // User-creatable child entities
    public int? DependsOnModuleQuota { get; set; }
    public int? ModuleExtraFileQuota { get; set; }
    public int? NamespaceExtraFileQuota { get; set; }
    public int? ModuleAdditionalTriggerPathQuota { get; set; }
    public int? NamespaceAdditionalTriggerPathQuota { get; set; }
    public int? ModuleTerraformInlinePolicyQuota { get; set; }
    public int? ModuleTerraformRemotePolicyQuota { get; set; }
    public int? ModuleTerraformLocalPolicyQuota { get; set; }
    public int? ModulePulumiInlinePolicyQuota { get; set; }
    public int? ModulePulumiRemotePolicyQuota { get; set; }
    public int? ModulePulumiLocalPolicyQuota { get; set; }
    public int? NamespaceTerraformInlinePolicyQuota { get; set; }
    public int? NamespaceTerraformRemotePolicyQuota { get; set; }
    public int? NamespaceTerraformLocalPolicyQuota { get; set; }
    public int? NamespacePulumiInlinePolicyQuota { get; set; }
    public int? NamespacePulumiRemotePolicyQuota { get; set; }
    public int? NamespacePulumiLocalPolicyQuota { get; set; }
    public int? ModuleBackendConfigQuota { get; set; }
    public int? NamespaceBackendConfigQuota { get; set; }
    public int? ModulePulumiFlagQuota { get; set; }
    public int? ModulePulumiArrayFlagQuota { get; set; }
    public int? NamespacePulumiFlagQuota { get; set; }
    public int? NamespacePulumiArrayFlagQuota { get; set; }
    public int? ModuleTerraformFlagQuota { get; set; }
    public int? ModuleTerraformArrayFlagQuota { get; set; }
    public int? NamespaceTerraformFlagQuota { get; set; }
    public int? NamespaceTerraformArrayFlagQuota { get; set; }
    public int? ModuleHookQuota { get; set; }
    public int? NamespaceHookQuota { get; set; }
    public int? SourceRefresherPreselectionQuota { get; set; }

    // Runner assignments
    public int? RunnerStackSupplyQuota { get; set; }
    public int? RunnerNamespaceSupplyQuota { get; set; }
    public int? RunnerModuleSupplyQuota { get; set; }

    // Agent assignments
    public int? AgentStackSupplyQuota { get; set; }
    public int? AgentNamespaceSupplyQuota { get; set; }
    public int? AgentModuleSupplyQuota { get; set; }

    // Group members
    public int? UserGroupMemberQuota { get; set; }
    public int? ServicePrincipalGroupMemberQuota { get; set; }
    public int? GroupGroupMemberQuota { get; set; }

    // Scoped secrets
    public int? StackSecretQuota { get; set; }
    public int? NamespaceSecretQuota { get; set; }
    public int? ModuleSecretQuota { get; set; }

    // ModuleInput types
    public int? ModuleParamFromDefinitionQuota { get; set; }
    public int? ModuleEnvVarFromDefinitionQuota { get; set; }
    public int? ModuleParamFromLiteralQuota { get; set; }
    public int? ModuleEnvVarFromLiteralQuota { get; set; }
    public int? ModuleParamFromSecretQuota { get; set; }
    public int? ModuleEnvVarFromSecretQuota { get; set; }
    public int? ModuleParamFromOutputQuota { get; set; }
    public int? ModuleEnvVarFromOutputQuota { get; set; }
    public int? ModuleParamFromOutputSetQuota { get; set; }
    public int? ModuleParamFromNamespaceQuota { get; set; }
    public int? ModuleEnvVarFromNamespaceQuota { get; set; }

    // NamespaceInput types
    public int? NamespaceParamFromDefinitionQuota { get; set; }
    public int? NamespaceEnvVarFromDefinitionQuota { get; set; }
    public int? NamespaceParamFromLiteralQuota { get; set; }
    public int? NamespaceEnvVarFromLiteralQuota { get; set; }
    public int? NamespaceParamFromSecretQuota { get; set; }
    public int? NamespaceEnvVarFromSecretQuota { get; set; }

    // Role assignments - Organization scope
    public int? UserOrganizationRoleAssignmentQuota { get; set; }
    public int? ServicePrincipalOrganizationRoleAssignmentQuota { get; set; }
    public int? GroupOrganizationRoleAssignmentQuota { get; set; }

    // Role assignments - Stack scope
    public int? UserStackRoleAssignmentQuota { get; set; }
    public int? ServicePrincipalStackRoleAssignmentQuota { get; set; }
    public int? GroupStackRoleAssignmentQuota { get; set; }

    // Role assignments - Namespace scope
    public int? UserNamespaceRoleAssignmentQuota { get; set; }
    public int? ServicePrincipalNamespaceRoleAssignmentQuota { get; set; }
    public int? GroupNamespaceRoleAssignmentQuota { get; set; }

    // Role assignments - Module scope
    public int? UserModuleRoleAssignmentQuota { get; set; }
    public int? ServicePrincipalModuleRoleAssignmentQuota { get; set; }
    public int? GroupModuleRoleAssignmentQuota { get; set; }

    // Role assignments - Runner scope
    public int? UserRunnerRoleAssignmentQuota { get; set; }
    public int? ServicePrincipalRunnerRoleAssignmentQuota { get; set; }
    public int? GroupRunnerRoleAssignmentQuota { get; set; }

    // Jobs
    public int? ModuleJobQuota { get; set; }

    // Drift check
    public int? MinDriftCheckIntervalMinutes { get; set; }
    public int? DefaultDriftCheckIntervalMinutes { get; set; }

    // Runner connections (concurrent runner instances)
    public int? RunnerConnectionQuota { get; set; }
    public int? RunnerConnectionJobQuota { get; set; }

    // StateStore + StateFile
    public int? StateStoreQuota { get; set; }
    public int? StateFileQuota { get; set; }

    // Role assignments - StateStore scope
    public int? UserStateStoreRoleAssignmentQuota { get; set; }
    public int? ServicePrincipalStateStoreRoleAssignmentQuota { get; set; }
    public int? GroupStateStoreRoleAssignmentQuota { get; set; }

    // Agent + missions + role assignments
    public int? AgentQuota { get; set; }
    public int? AgentConnectionQuota { get; set; }
    public int? OrganizationMissionQuota { get; set; }
    public int? StackMissionQuota { get; set; }
    public int? NamespaceMissionQuota { get; set; }
    public int? ModuleMissionQuota { get; set; }
    public int? UserAgentRoleAssignmentQuota { get; set; }
    public int? ServicePrincipalAgentRoleAssignmentQuota { get; set; }
    public int? GroupAgentRoleAssignmentQuota { get; set; }

    /// <summary>
    /// Get a specific quota by name using reflection.
    /// Returns null for unlimited.
    /// </summary>
    public int? GetQuota(string quotaName)
    {
        var property = GetType().GetProperty(quotaName);
        if (property != null)
        {
            return property.GetValue(this) as int?;
        }
        return DefaultQuota;
    }
}
