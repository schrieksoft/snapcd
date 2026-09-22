-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

-- ==========================================================================
-- Rebuilds the dependency graph's materialized tables from the live Modules, Namespaces,
-- Stacks, ModuleJobs and ModuleSagas rows.
--
-- Unconditional: a rebuild is also the repair for anything an older version left wrong.
--
-- sp_RecomputeModuleState truncates, so IsRunning and the saga headlines are re-derived from
-- whatever is in flight at the time.
-- ==========================================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

EXEC sp_RecomputeDependencyEdges;
GO

EXEC sp_RecomputeRecursiveDependencyEdges;
GO

EXEC sp_RecomputeModuleState;
GO
