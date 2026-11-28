-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

UPDATE s
SET Discriminator = N'StackSecret', ScopeKind = N'Stack', StackId = s.NamespaceId, NamespaceId = NULL
FROM dbo.Secrets s
WHERE s.Discriminator = N'NamespaceSecret' AND EXISTS (SELECT 1 FROM dbo.Stacks st WHERE st.Id = s.NamespaceId);
GO

DELETE x FROM dbo.NamespaceIntegrationEvents x WHERE EXISTS (SELECT 1 FROM dbo.Stacks st WHERE st.Id = x.NamespaceId);
DELETE x FROM dbo.NamespaceMissions x WHERE EXISTS (SELECT 1 FROM dbo.Stacks st WHERE st.Id = x.NamespaceId);
DELETE x FROM dbo.IntegrationNamespaceSupplies x WHERE EXISTS (SELECT 1 FROM dbo.Stacks st WHERE st.Id = x.NamespaceId);
DELETE x FROM dbo.AgentNamespaceSupplies x WHERE EXISTS (SELECT 1 FROM dbo.Stacks st WHERE st.Id = x.NamespaceId);
DELETE x FROM dbo.RunnerNamespaceSupplies x WHERE EXISTS (SELECT 1 FROM dbo.Stacks st WHERE st.Id = x.NamespaceId);
DELETE x FROM dbo.NamespaceRoleAssignments x WHERE EXISTS (SELECT 1 FROM dbo.Stacks st WHERE st.Id = x.NamespaceId);
GO

UPDATE n
SET ParentNamespaceId = NULL
FROM dbo.Namespaces n
WHERE n.ParentNamespaceId = n.StackId;
GO

DELETE n FROM dbo.Namespaces n WHERE n.Id = n.StackId;
GO
