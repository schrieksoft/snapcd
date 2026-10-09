-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

-- ==========================================================================
-- Backfill for both derivations.
--
-- A table type cannot be declared in the transaction that created it, so this
-- runs in its own migration.
-- ==========================================================================

SET QUOTED_IDENTIFIER ON;
GO

-- Step 4: nothing fires for principals who already qualify, so derive once.
DECLARE @Existing dbo.PrincipalScopeList;

INSERT INTO @Existing (PrincipalId, OrganizationId, PrincipalDiscriminator)
SELECT DISTINCT PrincipalId, OrganizationId, PrincipalDiscriminator
FROM (
    SELECT UserId AS PrincipalId, OrganizationId, 'User' AS PrincipalDiscriminator FROM dbo.OrganizationUsers
    UNION ALL
    SELECT Id, OrganizationId, 'ServicePrincipal' FROM dbo.ServicePrincipals
    UNION ALL
    SELECT Id, OrganizationId, 'Group' FROM dbo.Groups
) all_principals;

EXEC dbo.usp_SyncIdentityAccessMetadataReaders @Principals = @Existing;
GO

-- Nothing fires for what is already there. Empty lists mean everything, which
-- is what a first run needs and nothing else uses.
DECLARE @AllEntities dbo.DerivedEntityList;
DECLARE @AllPrincipals dbo.DerivedPrincipalList;
EXEC dbo.usp_SyncDerivedRunnerMetadataReaders @Entities = @AllEntities, @Principals = @AllPrincipals;
EXEC dbo.usp_SyncDerivedAgentMetadataReaders @Entities = @AllEntities, @Principals = @AllPrincipals;
EXEC dbo.usp_SyncDerivedIntegrationMetadataReaders @Entities = @AllEntities, @Principals = @AllPrincipals;
GO
