-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

-- Backfills SelectedRunnerId from the saga that still holds it.
--
-- A saga is deleted when its job finishes, so only unfinished jobs can be recovered. A finished
-- job keeps a null SelectedRunnerId, and the log endpoints refuse a null, which is correct: a
-- finished job has nothing left to log.

UPDATE j
SET j.SelectedRunnerId = s.RunnerId
FROM dbo.ModuleJobs j
INNER JOIN dbo.ApplyJobSagas s
    ON s.CorrelationId = j.Id AND s.OrganizationId = j.OrganizationId
WHERE j.SelectedRunnerId IS NULL;
GO

UPDATE j
SET j.SelectedRunnerId = s.RunnerId
FROM dbo.ModuleJobs j
INNER JOIN dbo.DestroyJobSagas s
    ON s.CorrelationId = j.Id AND s.OrganizationId = j.OrganizationId
WHERE j.SelectedRunnerId IS NULL;
GO

UPDATE j
SET j.SelectedRunnerId = s.RunnerId
FROM dbo.StateMigrationJobs j
INNER JOIN dbo.MoveSagas s
    ON s.CorrelationId = j.Id AND s.OrganizationId = j.OrganizationId
WHERE j.SelectedRunnerId IS NULL;
GO

UPDATE j
SET j.SelectedRunnerId = s.RunnerId
FROM dbo.StateMigrationJobs j
INNER JOIN dbo.ImportSagas s
    ON s.CorrelationId = j.Id AND s.OrganizationId = j.OrganizationId
WHERE j.SelectedRunnerId IS NULL;
GO

UPDATE j
SET j.SelectedRunnerId = s.RunnerId
FROM dbo.StateMigrationJobs j
INNER JOIN dbo.RemoveSagas s
    ON s.CorrelationId = j.Id AND s.OrganizationId = j.OrganizationId
WHERE j.SelectedRunnerId IS NULL;
GO

UPDATE j
SET j.SelectedRunnerId = s.RunnerId
FROM dbo.StateMigrationJobs j
INNER JOIN dbo.SplitMigrateSagas s
    ON s.CorrelationId = j.Id AND s.OrganizationId = j.OrganizationId
WHERE j.SelectedRunnerId IS NULL;
GO

UPDATE j
SET j.SelectedRunnerId = s.RunnerId
FROM dbo.StateMigrationJobs j
INNER JOIN dbo.TransferMigrateSagas s
    ON s.CorrelationId = j.Id AND s.OrganizationId = j.OrganizationId
WHERE j.SelectedRunnerId IS NULL;
GO

UPDATE j
SET j.SelectedRunnerId = s.RunnerId
FROM dbo.StateMigrationJobs j
INNER JOIN dbo.LookupAddressesSagas s
    ON s.CorrelationId = j.Id AND s.OrganizationId = j.OrganizationId
WHERE j.SelectedRunnerId IS NULL;
GO

UPDATE j
SET j.SelectedRunnerId = s.RunnerId
FROM dbo.RippleProbeJobs j
INNER JOIN dbo.RippleProbeSagas s
    ON s.CorrelationId = j.Id AND s.OrganizationId = j.OrganizationId
WHERE j.SelectedRunnerId IS NULL;
