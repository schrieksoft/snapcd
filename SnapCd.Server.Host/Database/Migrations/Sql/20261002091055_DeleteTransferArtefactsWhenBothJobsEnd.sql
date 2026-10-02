-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

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
