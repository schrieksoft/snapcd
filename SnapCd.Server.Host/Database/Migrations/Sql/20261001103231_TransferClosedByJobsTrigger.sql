-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

-- ==========================================================================
-- The trigger that closes a transfer once neither of its jobs is still running.
--
-- A transfer claims both its Modules in the statement that creates it, so the claim cannot fail
-- to happen. Releasing them has to be the same kind of thing. Here that means a transfer whose
-- jobs have all ended closes itself, and trg_Transfers_TransferLocks then drops the locks.
--
-- Whether the jobs succeeded is not asked: a further attempt is a new transfer, so what matters
-- is that neither Module is still busy under this one.
--
-- One ended job with no second one is also closed. That is the initiator failing while the
-- counterparty has not answered, and nothing will ever start the counterparty's side: consent is
-- what does that, and the side it would join is already dead. Holding the lock there would leave
-- both Modules claimed by a transfer that cannot proceed, and would let a later consent start a
-- state move against a failed partner.
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

    IF NOT UPDATE(Status) AND NOT EXISTS (SELECT 1 FROM deleted) RETURN;

    UPDATE t
    SET ClosedAt = SYSDATETIMEOFFSET(),
        CloseReason = 'both modules finished'
    FROM Transfers t
    WHERE t.ClosedAt IS NULL
      AND EXISTS (
          SELECT 1 FROM inserted i
          WHERE i.TransferId = t.Id AND i.OrganizationId = t.OrganizationId)
      -- Nothing of this transfer is still in flight. Whether one side or both have jobs, a
      -- transfer with no running job has no way to make progress.
      AND NOT EXISTS (
          SELECT 1 FROM StateMigrationJobs j
          WHERE j.TransferId = t.Id AND j.OrganizationId = t.OrganizationId
            AND j.Status = 'Running');
END;
GO
