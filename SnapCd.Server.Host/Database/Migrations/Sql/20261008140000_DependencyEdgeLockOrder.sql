-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

-- Writing a DependencyEdges row touches the clustered key and both nonclustered indexes, so two
-- statements that reach the same row through different indexes take its locks in opposite order
-- and deadlock. Every statement here seeks PK_DependencyEdges, and the hints hold it to that.

SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE sp_UpdateDependencyEdgesForModules
    @AffectedEdges dbo.ModuleEdgeList READONLY
AS
BEGIN
    SET NOCOUNT ON;

    -- The desired edges among the pairs being reconciled.
    DECLARE @desired TABLE (
        DefinedModuleId UNIQUEIDENTIFIER NOT NULL,
        ReferencedModuleId UNIQUEIDENTIFIER NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NOT NULL,
        PRIMARY KEY (DefinedModuleId, ReferencedModuleId));

    INSERT INTO @desired (DefinedModuleId, ReferencedModuleId, OrganizationId)
    SELECT s.DefinedModuleId, s.ReferencedModuleId, MIN(s.OrganizationId)
    FROM (
        SELECT ModuleId AS DefinedModuleId, DependsOnModuleId AS ReferencedModuleId, OrganizationId
        FROM DependsOnModules
        UNION
        SELECT ModuleId AS DefinedModuleId, OutputModuleId AS ReferencedModuleId, OrganizationId
        FROM ModuleInputs
        WHERE Discriminator IN ('ModuleEnvVarFromOutput', 'ModuleParamFromOutput', 'ModuleParamFromOutputSet')
    ) s
    JOIN @AffectedEdges e
      ON e.DefinedModuleId = s.DefinedModuleId AND e.ReferencedModuleId = s.ReferencedModuleId
    GROUP BY s.DefinedModuleId, s.ReferencedModuleId;

    -- Delete, update, then insert, each seeking single keys on the clustered index.
    DELETE t
    FROM @AffectedEdges e
    JOIN DependencyEdges t WITH (INDEX(PK_DependencyEdges))
      ON t.DefinedModuleId = e.DefinedModuleId AND t.ReferencedModuleId = e.ReferencedModuleId
    WHERE NOT EXISTS (SELECT 1 FROM @desired d
                      WHERE d.DefinedModuleId = t.DefinedModuleId
                        AND d.ReferencedModuleId = t.ReferencedModuleId)
    OPTION (FORCE ORDER, LOOP JOIN);

    UPDATE t
    SET OrganizationId = d.OrganizationId
    FROM @desired d
    JOIN DependencyEdges t WITH (INDEX(PK_DependencyEdges))
      ON t.DefinedModuleId = d.DefinedModuleId AND t.ReferencedModuleId = d.ReferencedModuleId
    WHERE t.OrganizationId <> d.OrganizationId
    OPTION (FORCE ORDER, LOOP JOIN);

    INSERT INTO DependencyEdges (DefinedModuleId, ReferencedModuleId, OrganizationId)
    SELECT d.DefinedModuleId, d.ReferencedModuleId, d.OrganizationId
    FROM @desired d
    WHERE NOT EXISTS (SELECT 1 FROM DependencyEdges t WITH (UPDLOCK, HOLDLOCK, INDEX(PK_DependencyEdges))
                      WHERE t.DefinedModuleId = d.DefinedModuleId
                        AND t.ReferencedModuleId = d.ReferencedModuleId)
    ORDER BY d.DefinedModuleId, d.ReferencedModuleId;
END;
GO
