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
