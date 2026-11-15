-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

-- ==========================================================================
-- Each Namespace's defaults as its subtree sees them: the nearest Namespace
-- that sets a value, unless one above it pins the value.
--
-- A view rather than a table, so there is nothing to invalidate. It reads
-- NamespaceAncestors, which the triggers keep current.
--
-- Two kinds of pin. Forbidden fixes the value for the whole subtree. For a
-- setting whose values order from permissive to strict, OnlyStricter instead
-- sets a floor: a Namespace below may tighten the value but not loosen it.
--
-- Each setting resolves through three aggregates over the ancestor rows:
--   ForbidDepth  the furthest ancestor that forbids an override and sets a value
--   Base         that ancestor's value, or the nearest set value where none does
--   Floor        the strictest OnlyStricter value above ForbidDepth
-- and the answer is whichever of Base and Floor is stricter. A floor set above
-- a forbidding Namespace still applies to it, so Forbidden cannot loosen what
-- an ancestor already required.
-- ==========================================================================

SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER VIEW dbo.NamespaceResolvedDefaults
AS
WITH Chain AS
(
    SELECT
        a.NamespaceId,
        a.OrganizationId,
        a.Depth,

        CAST(n.DefaultCleanInitEnabled AS INT) AS CleanInitValue,
        n.OverrideOnCleanInitEnabled AS CleanInitPolicy,
        CAST(n.DefaultTriggerPathFilterEnabled AS INT) AS TriggerPathFilterValue,
        n.OverrideOnTriggerPathFilterEnabled AS TriggerPathFilterPolicy,
        CAST(n.DefaultDriftCheckEnabled AS INT) AS DriftCheckValue,
        n.OverrideOnDriftCheckEnabled AS DriftCheckPolicy,
        n.DefaultApplyApprovalThreshold AS ApplyValue,
        n.OverrideOnApplyApprovalThreshold AS ApplyPolicy,
        n.DefaultDestroyApprovalThreshold AS DestroyValue,
        n.OverrideOnDestroyApprovalThreshold AS DestroyPolicy,
        n.DefaultStateMigrationApprovalThreshold AS StateMigrationValue,
        n.OverrideOnStateMigrationApprovalThreshold AS StateMigrationPolicy,
        CASE n.DefaultOutputSetDownstreamRestriction WHEN 'SensitiveOnly' THEN 0 WHEN 'All' THEN 1 END AS RestrictionValue,
        n.OverrideOnOutputSetDownstreamRestriction AS RestrictionPolicy,
        CASE n.DefaultOutputRestrictionExemption WHEN 'OwnNamespaceAndDescendants' THEN 0 WHEN 'OwnNamespace' THEN 1 WHEN 'Nobody' THEN 2 END AS ExemptionValue,
        n.OverrideOnOutputRestrictionExemption AS ExemptionPolicy,
        n.DefaultApprovalTimeoutMinutes AS TimeoutValue,
        n.OverrideOnApprovalTimeoutMinutes AS TimeoutPolicy,
        n.DefaultEngine AS EngineValue,
        n.OverrideOnEngine AS EnginePolicy,
        n.DefaultDriftCheckIntervalMinutes AS DriftIntervalValue,
        n.OverrideOnDriftCheckIntervalMinutes AS DriftIntervalPolicy
    FROM dbo.NamespaceAncestors a
    INNER JOIN dbo.Namespaces n
        ON n.Id = a.AncestorNamespaceId
        AND n.OrganizationId = a.OrganizationId
),
Pinned AS
(
    SELECT
        NamespaceId,
        OrganizationId,
        Depth,

        CleanInitValue,
        CleanInitPolicy,
        MAX(CASE WHEN CleanInitValue IS NOT NULL AND CleanInitPolicy = 'Forbidden' THEN Depth END)
            OVER (PARTITION BY NamespaceId, OrganizationId) AS CleanInitForbidDepth,
        TriggerPathFilterValue,
        TriggerPathFilterPolicy,
        MAX(CASE WHEN TriggerPathFilterValue IS NOT NULL AND TriggerPathFilterPolicy = 'Forbidden' THEN Depth END)
            OVER (PARTITION BY NamespaceId, OrganizationId) AS TriggerPathFilterForbidDepth,
        DriftCheckValue,
        DriftCheckPolicy,
        MAX(CASE WHEN DriftCheckValue IS NOT NULL AND DriftCheckPolicy = 'Forbidden' THEN Depth END)
            OVER (PARTITION BY NamespaceId, OrganizationId) AS DriftCheckForbidDepth,
        ApplyValue,
        ApplyPolicy,
        MAX(CASE WHEN ApplyValue IS NOT NULL AND ApplyPolicy = 'Forbidden' THEN Depth END)
            OVER (PARTITION BY NamespaceId, OrganizationId) AS ApplyForbidDepth,
        DestroyValue,
        DestroyPolicy,
        MAX(CASE WHEN DestroyValue IS NOT NULL AND DestroyPolicy = 'Forbidden' THEN Depth END)
            OVER (PARTITION BY NamespaceId, OrganizationId) AS DestroyForbidDepth,
        StateMigrationValue,
        StateMigrationPolicy,
        MAX(CASE WHEN StateMigrationValue IS NOT NULL AND StateMigrationPolicy = 'Forbidden' THEN Depth END)
            OVER (PARTITION BY NamespaceId, OrganizationId) AS StateMigrationForbidDepth,
        RestrictionValue,
        RestrictionPolicy,
        MAX(CASE WHEN RestrictionValue IS NOT NULL AND RestrictionPolicy = 'Forbidden' THEN Depth END)
            OVER (PARTITION BY NamespaceId, OrganizationId) AS RestrictionForbidDepth,
        ExemptionValue,
        ExemptionPolicy,
        MAX(CASE WHEN ExemptionValue IS NOT NULL AND ExemptionPolicy = 'Forbidden' THEN Depth END)
            OVER (PARTITION BY NamespaceId, OrganizationId) AS ExemptionForbidDepth,
        TimeoutValue,
        TimeoutPolicy,
        MAX(CASE WHEN TimeoutValue IS NOT NULL AND TimeoutPolicy = 'Forbidden' THEN Depth END)
            OVER (PARTITION BY NamespaceId, OrganizationId) AS TimeoutForbidDepth,
        EngineValue,
        EnginePolicy,
        MAX(CASE WHEN EngineValue IS NOT NULL AND EnginePolicy = 'Forbidden' THEN Depth END)
            OVER (PARTITION BY NamespaceId, OrganizationId) AS EngineForbidDepth,
        DriftIntervalValue,
        DriftIntervalPolicy,
        MAX(CASE WHEN DriftIntervalValue IS NOT NULL AND DriftIntervalPolicy = 'Forbidden' THEN Depth END)
            OVER (PARTITION BY NamespaceId, OrganizationId) AS DriftIntervalForbidDepth
    FROM Chain
),
Ranked AS
(
    SELECT
        NamespaceId,
        OrganizationId,
        Depth,

        CleanInitValue,
        CleanInitPolicy,
        CleanInitForbidDepth,
        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN CleanInitValue IS NULL THEN 2
                          WHEN Depth = CleanInitForbidDepth THEN 0 ELSE 1 END,
                     Depth) AS CleanInitRank,
        TriggerPathFilterValue,
        TriggerPathFilterPolicy,
        TriggerPathFilterForbidDepth,
        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN TriggerPathFilterValue IS NULL THEN 2
                          WHEN Depth = TriggerPathFilterForbidDepth THEN 0 ELSE 1 END,
                     Depth) AS TriggerPathFilterRank,
        DriftCheckValue,
        DriftCheckPolicy,
        DriftCheckForbidDepth,
        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN DriftCheckValue IS NULL THEN 2
                          WHEN Depth = DriftCheckForbidDepth THEN 0 ELSE 1 END,
                     Depth) AS DriftCheckRank,
        ApplyValue,
        ApplyPolicy,
        ApplyForbidDepth,
        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN ApplyValue IS NULL THEN 2
                          WHEN Depth = ApplyForbidDepth THEN 0 ELSE 1 END,
                     Depth) AS ApplyRank,
        DestroyValue,
        DestroyPolicy,
        DestroyForbidDepth,
        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN DestroyValue IS NULL THEN 2
                          WHEN Depth = DestroyForbidDepth THEN 0 ELSE 1 END,
                     Depth) AS DestroyRank,
        StateMigrationValue,
        StateMigrationPolicy,
        StateMigrationForbidDepth,
        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN StateMigrationValue IS NULL THEN 2
                          WHEN Depth = StateMigrationForbidDepth THEN 0 ELSE 1 END,
                     Depth) AS StateMigrationRank,
        RestrictionValue,
        RestrictionPolicy,
        RestrictionForbidDepth,
        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN RestrictionValue IS NULL THEN 2
                          WHEN Depth = RestrictionForbidDepth THEN 0 ELSE 1 END,
                     Depth) AS RestrictionRank,
        ExemptionValue,
        ExemptionPolicy,
        ExemptionForbidDepth,
        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN ExemptionValue IS NULL THEN 2
                          WHEN Depth = ExemptionForbidDepth THEN 0 ELSE 1 END,
                     Depth) AS ExemptionRank,
        TimeoutValue,
        TimeoutPolicy,
        TimeoutForbidDepth,
        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN TimeoutValue IS NULL THEN 2
                          WHEN Depth = TimeoutForbidDepth THEN 0 ELSE 1 END,
                     Depth) AS TimeoutRank,
        EngineValue,
        EnginePolicy,
        EngineForbidDepth,
        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN EngineValue IS NULL THEN 2
                          WHEN Depth = EngineForbidDepth THEN 0 ELSE 1 END,
                     Depth) AS EngineRank,
        DriftIntervalValue,
        DriftIntervalPolicy,
        DriftIntervalForbidDepth,
        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN DriftIntervalValue IS NULL THEN 2
                          WHEN Depth = DriftIntervalForbidDepth THEN 0 ELSE 1 END,
                     Depth) AS DriftIntervalRank
    FROM Pinned
),
Aggregated AS
(
    SELECT
        NamespaceId,
        OrganizationId,

        MAX(CASE WHEN CleanInitRank = 1 THEN CleanInitValue END) AS CleanInitBase,
        MAX(CASE WHEN TriggerPathFilterRank = 1 THEN TriggerPathFilterValue END) AS TriggerPathFilterBase,
        MAX(CASE WHEN DriftCheckRank = 1 THEN DriftCheckValue END) AS DriftCheckBase,
        MAX(CASE WHEN ApplyRank = 1 THEN ApplyValue END) AS ApplyBase,
        MAX(CASE WHEN DestroyRank = 1 THEN DestroyValue END) AS DestroyBase,
        MAX(CASE WHEN StateMigrationRank = 1 THEN StateMigrationValue END) AS StateMigrationBase,
        MAX(CASE WHEN RestrictionRank = 1 THEN RestrictionValue END) AS RestrictionBase,
        MAX(CASE WHEN ExemptionRank = 1 THEN ExemptionValue END) AS ExemptionBase,
        MAX(CASE WHEN TimeoutRank = 1 THEN TimeoutValue END) AS TimeoutBase,
        MAX(CASE WHEN EngineRank = 1 THEN EngineValue END) AS EngineBase,
        MAX(CASE WHEN DriftIntervalRank = 1 THEN DriftIntervalValue END) AS DriftIntervalBase,
        MAX(CASE WHEN CleanInitPolicy = 'OnlyStricter'
                  AND Depth > ISNULL(CleanInitForbidDepth, -1)
                 THEN CleanInitValue END) AS CleanInitFloor,
        MAX(CASE WHEN TriggerPathFilterPolicy = 'OnlyStricter'
                  AND Depth > ISNULL(TriggerPathFilterForbidDepth, -1)
                 THEN TriggerPathFilterValue END) AS TriggerPathFilterFloor,
        MAX(CASE WHEN DriftCheckPolicy = 'OnlyStricter'
                  AND Depth > ISNULL(DriftCheckForbidDepth, -1)
                 THEN DriftCheckValue END) AS DriftCheckFloor,
        MAX(CASE WHEN ApplyPolicy = 'OnlyStricter'
                  AND Depth > ISNULL(ApplyForbidDepth, -1)
                 THEN ApplyValue END) AS ApplyFloor,
        MAX(CASE WHEN DestroyPolicy = 'OnlyStricter'
                  AND Depth > ISNULL(DestroyForbidDepth, -1)
                 THEN DestroyValue END) AS DestroyFloor,
        MAX(CASE WHEN StateMigrationPolicy = 'OnlyStricter'
                  AND Depth > ISNULL(StateMigrationForbidDepth, -1)
                 THEN StateMigrationValue END) AS StateMigrationFloor,
        MAX(CASE WHEN RestrictionPolicy = 'OnlyStricter'
                  AND Depth > ISNULL(RestrictionForbidDepth, -1)
                 THEN RestrictionValue END) AS RestrictionFloor,
        MAX(CASE WHEN ExemptionPolicy = 'OnlyStricter'
                  AND Depth > ISNULL(ExemptionForbidDepth, -1)
                 THEN ExemptionValue END) AS ExemptionFloor
    FROM Ranked
    GROUP BY NamespaceId, OrganizationId
)
SELECT
    NamespaceId,
    OrganizationId,
    CAST(IIF(CleanInitFloor > CleanInitBase, CleanInitFloor, CleanInitBase) AS BIT) AS CleanInitEnabled,
    CAST(IIF(TriggerPathFilterFloor > TriggerPathFilterBase, TriggerPathFilterFloor, TriggerPathFilterBase) AS BIT) AS TriggerPathFilterEnabled,
    CAST(IIF(DriftCheckFloor > DriftCheckBase, DriftCheckFloor, DriftCheckBase) AS BIT) AS DriftCheckEnabled,
    IIF(ApplyFloor > ApplyBase, ApplyFloor, ApplyBase) AS ApplyApprovalThreshold,
    IIF(DestroyFloor > DestroyBase, DestroyFloor, DestroyBase) AS DestroyApprovalThreshold,
    IIF(StateMigrationFloor > StateMigrationBase, StateMigrationFloor, StateMigrationBase) AS StateMigrationApprovalThreshold,
    CASE IIF(RestrictionFloor > RestrictionBase, RestrictionFloor, RestrictionBase) WHEN 0 THEN 'SensitiveOnly' WHEN 1 THEN 'All' END AS OutputSetDownstreamRestriction,
    CASE IIF(ExemptionFloor > ExemptionBase, ExemptionFloor, ExemptionBase) WHEN 0 THEN 'OwnNamespaceAndDescendants' WHEN 1 THEN 'OwnNamespace' WHEN 2 THEN 'Nobody' END AS OutputRestrictionExemption,
    TimeoutBase AS ApprovalTimeoutMinutes,
    EngineBase AS Engine,
    DriftIntervalBase AS DriftCheckIntervalMinutes
FROM Aggregated;
GO
