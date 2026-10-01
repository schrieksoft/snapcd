-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

-- ==========================================================================
-- Closes a transfer as soon as any of its jobs reaches a terminal status.
--
-- A transfer links two jobs. It does not keep a Module free for one: a Module already running a
-- state migration refuses the next one on its own, in StateMigrationService. So there is nothing a
-- transfer holds open once a side has ended, and a further attempt is a new transfer either way.
--
-- The condition reads only `inserted`, so closing never depends on a row another transaction owns.
--
-- Running is the only non-terminal ExecutionStatus, so `<> 'Running'` is every ended job: it covers
-- a fault and a cancel as well as a clean finish.
--
-- trg_Transfers_TransferLocks drops the locks from the ClosedAt write, as it already did.
--
-- A snapshot, not a live definition: editing it changes nothing on a database that has run the
-- migration. Change the trigger by adding a new migration with its own SQL file.
-- ==========================================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

CREATE OR ALTER TRIGGER trg_StateMigrationJobs_CloseTransfer
ON StateMigrationJobs
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE t
    SET ClosedAt = SYSDATETIMEOFFSET(),
        CloseReason = 'a module finished'
    FROM Transfers t
    WHERE t.ClosedAt IS NULL
      AND EXISTS (
          SELECT 1 FROM inserted i
          WHERE i.TransferId = t.Id
            AND i.OrganizationId = t.OrganizationId
            AND i.Status <> 'Running');
END;
GO
