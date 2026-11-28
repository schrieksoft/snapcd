-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

-- Each Stack gets a root Namespace carrying the Stack's own id, and what belonged to the Stack is
-- given to that Namespace. The Stack rows stay.

INSERT INTO dbo.Namespaces
    (Id, OrganizationId, StackId, ParentNamespaceId, Name, TriggerBehaviourOnModified,
     DefaultOutputSetDownstreamRestriction, DefaultOutputRestrictionExemption,
     IgnoreInheritedExtraFiles, IgnoreInheritedFlags, IgnoreInheritedHooks, IgnoreInheritedIntegrationEvents,
     IgnoreInheritedMissions, IgnoreInheritedPolicies, IgnoreInheritedTriggerPaths,
     CreatedBy, CreatedByAgentId, CreatedByPrincipalDiscriminator, CreatedDateTime,
     ModifiedBy, ModifiedByAgentId, ModifiedByPrincipalDiscriminator, ModifiedDateTime)
SELECT s.Id, s.OrganizationId, s.Id, NULL, s.Name, s.TriggerBehaviourOnModified,
       N'All', N'OwnNamespaceAndDescendants',
       0, 0, 0, 0, 0, 0, 0,
       s.CreatedBy, s.CreatedByAgentId, s.CreatedByPrincipalDiscriminator, s.CreatedDateTime,
       s.ModifiedBy, s.ModifiedByAgentId, s.ModifiedByPrincipalDiscriminator, s.ModifiedDateTime
FROM dbo.Stacks s
WHERE NOT EXISTS (SELECT 1 FROM dbo.Namespaces n WHERE n.Id = s.Id);
GO

UPDATE n
SET ParentNamespaceId = n.StackId
FROM dbo.Namespaces n
WHERE n.ParentNamespaceId IS NULL AND n.Id <> n.StackId;
GO

-- Each copy keeps the id of the row it was made from, so an id held outside the server still resolves.
-- Contributor on a Stack could change the Namespaces in it, which on a Namespace is a second, nested role.
INSERT INTO dbo.NamespaceRoleAssignments
    (Id, OrganizationId, NamespaceId, PrincipalDiscriminator, UserId, GroupId, ServicePrincipalId, RoleName,
     CreatedBy, CreatedByAgentId, CreatedByPrincipalDiscriminator, CreatedDateTime,
     ModifiedBy, ModifiedByAgentId, ModifiedByPrincipalDiscriminator, ModifiedDateTime)
SELECT CASE WHEN m.KeepsId = 1 THEN ra.Id ELSE NEWID() END, ra.OrganizationId, ra.StackId, ra.PrincipalDiscriminator, ra.UserId, ra.GroupId, ra.ServicePrincipalId, m.NamespaceRole,
       ra.CreatedBy, ra.CreatedByAgentId, ra.CreatedByPrincipalDiscriminator, ra.CreatedDateTime,
       ra.ModifiedBy, ra.ModifiedByAgentId, ra.ModifiedByPrincipalDiscriminator, ra.ModifiedDateTime
FROM dbo.StackRoleAssignments ra
JOIN (VALUES
        (N'Owner', N'Owner', 1),
        (N'Contributor', N'Contributor', 1),
        (N'Contributor', N'NestedNamespaceContributor', 0),
        (N'Reader', N'Reader', 1),
        (N'MetadataReader', N'MetadataReader', 1),
        (N'NamespaceCreator', N'NestedNamespaceCreator', 1),
        (N'IdentityAccessManager', N'IdentityAccessManager', 1),
        (N'JobManager', N'JobManager', 1)
     ) m (StackRole, NamespaceRole, KeepsId) ON m.StackRole = ra.RoleName;
GO

-- A supply to a Stack reaches every Namespace in it, so the copy is inherited.
INSERT INTO dbo.RunnerNamespaceSupplies
    (Id, OrganizationId, RunnerId, NamespaceId, InheritedByDescendants,
     CreatedBy, CreatedByAgentId, CreatedByPrincipalDiscriminator, CreatedDateTime,
     ModifiedBy, ModifiedByAgentId, ModifiedByPrincipalDiscriminator, ModifiedDateTime)
SELECT x.Id, x.OrganizationId, x.RunnerId, x.StackId, 1,
       x.CreatedBy, x.CreatedByAgentId, x.CreatedByPrincipalDiscriminator, x.CreatedDateTime,
       x.ModifiedBy, x.ModifiedByAgentId, x.ModifiedByPrincipalDiscriminator, x.ModifiedDateTime
FROM dbo.RunnerStackSupplies x;
GO

INSERT INTO dbo.AgentNamespaceSupplies
    (Id, OrganizationId, AgentId, NamespaceId, InheritedByDescendants,
     CreatedBy, CreatedByAgentId, CreatedByPrincipalDiscriminator, CreatedDateTime,
     ModifiedBy, ModifiedByAgentId, ModifiedByPrincipalDiscriminator, ModifiedDateTime)
SELECT x.Id, x.OrganizationId, x.AgentId, x.StackId, 1,
       x.CreatedBy, x.CreatedByAgentId, x.CreatedByPrincipalDiscriminator, x.CreatedDateTime,
       x.ModifiedBy, x.ModifiedByAgentId, x.ModifiedByPrincipalDiscriminator, x.ModifiedDateTime
FROM dbo.AgentStackSupplies x;
GO

INSERT INTO dbo.IntegrationNamespaceSupplies
    (Id, OrganizationId, IntegrationId, NamespaceId, InheritedByDescendants,
     CreatedBy, CreatedByAgentId, CreatedByPrincipalDiscriminator, CreatedDateTime,
     ModifiedBy, ModifiedByAgentId, ModifiedByPrincipalDiscriminator, ModifiedDateTime)
SELECT x.Id, x.OrganizationId, x.IntegrationId, x.StackId, 1,
       x.CreatedBy, x.CreatedByAgentId, x.CreatedByPrincipalDiscriminator, x.CreatedDateTime,
       x.ModifiedBy, x.ModifiedByAgentId, x.ModifiedByPrincipalDiscriminator, x.ModifiedDateTime
FROM dbo.IntegrationStackSupplies x;
GO

INSERT INTO dbo.NamespaceMissions
    (Id, OrganizationId, AgentId, NamespaceId, MissionType, SidecarName, IsDisabled,
     CreatedBy, CreatedByAgentId, CreatedByPrincipalDiscriminator, CreatedDateTime,
     ModifiedBy, ModifiedByAgentId, ModifiedByPrincipalDiscriminator, ModifiedDateTime)
SELECT x.Id, x.OrganizationId, x.AgentId, x.StackId, x.MissionType, x.SidecarName, x.IsDisabled,
       x.CreatedBy, x.CreatedByAgentId, x.CreatedByPrincipalDiscriminator, x.CreatedDateTime,
       x.ModifiedBy, x.ModifiedByAgentId, x.ModifiedByPrincipalDiscriminator, x.ModifiedDateTime
FROM dbo.StackMissions x;
GO

INSERT INTO dbo.NamespaceIntegrationEvents
    (Id, OrganizationId, IntegrationId, NamespaceId, [Trigger], Template, Filter, IsDisabled,
     CreatedBy, CreatedByAgentId, CreatedByPrincipalDiscriminator, CreatedDateTime,
     ModifiedBy, ModifiedByAgentId, ModifiedByPrincipalDiscriminator, ModifiedDateTime)
SELECT x.Id, x.OrganizationId, x.IntegrationId, x.StackId, x.[Trigger], x.Template, x.Filter, x.IsDisabled,
       x.CreatedBy, x.CreatedByAgentId, x.CreatedByPrincipalDiscriminator, x.CreatedDateTime,
       x.ModifiedBy, x.ModifiedByAgentId, x.ModifiedByPrincipalDiscriminator, x.ModifiedDateTime
FROM dbo.StackIntegrationEvents x;
GO

-- A Secret's value is stored in the vault under its own id, so the row is kept and changes owner.
UPDATE dbo.Secrets
SET Discriminator = N'NamespaceSecret', NamespaceId = StackId, StackId = NULL
WHERE Discriminator = N'StackSecret';
GO
