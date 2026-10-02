-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

-- ==========================================================================
-- The trigger that maintains TransferLocks from Transfers.
--
-- A Module is in at most one open transfer, in either role. Two filtered unique indexes on
-- Transfers cannot say that, because the pair lives in two columns of one row, so the rule is
-- carried by TransferLocks' primary key instead: one row per Module.
--
-- The locks are never written by hand. An open transfer claims both its Modules here, and a
-- closed one releases them, so the claim cannot drift from the row it comes from.
--
-- A snapshot, not a live definition: editing it changes nothing on a database that has run the
-- migration. Change the trigger by adding a new migration with its own SQL file.
-- ==========================================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

CREATE OR ALTER TRIGGER trg_Transfers_TransferLocks
ON Transfers
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- A closed transfer releases both its Modules.
    DELETE l
    FROM TransferLocks l
    JOIN inserted i ON i.Id = l.TransferId AND i.OrganizationId = l.OrganizationId
    WHERE i.ClosedAt IS NOT NULL;

    -- An open one claims them. A Module already claimed by another transfer violates the primary
    -- key, which rolls the whole statement back: the refusal is the constraint, not a check that
    -- could read stale rows.
    INSERT INTO TransferLocks (OrganizationId, ModuleId, TransferId)
    SELECT i.OrganizationId, m.ModuleId, i.Id
    FROM inserted i
    CROSS APPLY (VALUES (i.ModuleId), (i.CounterpartyModuleId)) AS m(ModuleId)
    WHERE i.ClosedAt IS NULL
      AND NOT EXISTS (
          SELECT 1 FROM TransferLocks l
          WHERE l.OrganizationId = i.OrganizationId
            AND l.ModuleId = m.ModuleId
            AND l.TransferId = i.Id);
END;
GO

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

-- ==========================================================================
-- Deletes a transfer's artefacts once neither of its jobs is still running.
--
-- The artefacts are what one half produces for the other: the state fragment, its meta, and the
-- output values the counterpart's plan reads. They are raw state, so they are not kept beyond the
-- transfer that needed them. A further attempt is a new transfer with a fragment cut from state as
-- it then stands, so nothing reads these afterwards.
--
-- Running is the only non-terminal ExecutionStatus, so `<> 'Running'` is every ended job: a failed
-- or cancelled half counts as ended. The state each side wrote is in the state store either way;
-- a fragment is how it travelled, not a record of it.
--
-- This is not the close. A transfer closes when the FIRST of its jobs ends, which releases the
-- Modules; the artefacts have to outlive that, because the other half is still running and still
-- needs what the first produced.
--
-- Artefacts exist only once consent has been granted and a half has run its map step, so a
-- transfer with one job row and no second one yet cannot reach this condition with artefacts
-- present: the asking side waits for the answer before it does anything.
--
-- DELETE as well as UPDATE: deleting a Module cascades to its StateMigrationJobs rows, so the last
-- job of a transfer can disappear without ever transitioning to a terminal status. Without the
-- delete arm those artefacts would never be removed.
--
-- A snapshot, not a live definition: editing it changes nothing on a database that has run the
-- migration. Change the trigger by adding a new migration with its own SQL file.
-- ==========================================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

CREATE OR ALTER TRIGGER trg_StateMigrationJobs_DeleteTransferArtefacts
ON StateMigrationJobs
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DELETE a
    FROM TransferArtefacts a
    WHERE EXISTS (
              SELECT 1
              FROM (SELECT TransferId, OrganizationId FROM inserted
                    UNION ALL
                    SELECT TransferId, OrganizationId FROM deleted) touched
              WHERE touched.TransferId = a.TransferId
                AND touched.OrganizationId = a.OrganizationId)
      AND NOT EXISTS (
              SELECT 1
              FROM StateMigrationJobs j
              WHERE j.TransferId = a.TransferId
                AND j.OrganizationId = a.OrganizationId
                AND j.Status = 'Running');
END;
GO

