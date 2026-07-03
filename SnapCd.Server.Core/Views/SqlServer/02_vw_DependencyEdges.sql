-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

CREATE OR ALTER VIEW vw_DependencyEdges AS
    SELECT
        dm.ModuleId              AS DefinedModuleId,
        dm.OrganizationId        AS DefinedOrganizationId,
        DefinedModules.Name      AS DefinedModuleName,
        DefinedModules.NamespaceId AS DefinedNamespaceId,
        DefinedNamespaces.Name   AS DefinedNamespaceName,
        DefinedNamespaces.StackId AS DefinedStackId,
        DefinedStacks.Name       AS DefinedStackName,
        CONCAT(DefinedStacks.Name, '/', DefinedNamespaces.Name, '/', DefinedModules.Name) AS DefinedDisplayName,

        dm.DependsOnModuleId     AS ReferencedModuleId,
        ReferencedModules.OrganizationId AS ReferencedOrganizationId,
        ReferencedModules.Name   AS ReferencedModuleName,
        ReferencedModules.NamespaceId AS ReferencedNamespaceId,
        ReferencedNamespaces.Name AS ReferencedNamespaceName,
        ReferencedNamespaces.StackId AS ReferencedStackId,
        ReferencedStacks.Name    AS ReferencedStackName,
        CONCAT(ReferencedStacks.Name, '/', ReferencedNamespaces.Name, '/', ReferencedModules.Name) AS ReferencedDisplayName

    FROM DependsOnModules dm
    INNER JOIN Modules DefinedModules ON dm.ModuleId = DefinedModules.Id
    INNER JOIN Namespaces DefinedNamespaces ON DefinedModules.NamespaceId = DefinedNamespaces.Id
    INNER JOIN Stacks DefinedStacks ON DefinedNamespaces.StackId = DefinedStacks.Id
    INNER JOIN Modules ReferencedModules ON dm.DependsOnModuleId = ReferencedModules.Id
    INNER JOIN Namespaces ReferencedNamespaces ON ReferencedModules.NamespaceId = ReferencedNamespaces.Id
    INNER JOIN Stacks ReferencedStacks ON ReferencedNamespaces.StackId = ReferencedStacks.Id

    UNION

    SELECT
        mi.ModuleId              AS DefinedModuleId,
        DefinedModules.OrganizationId AS DefinedOrganizationId,
        DefinedModules.Name      AS DefinedModuleName,
        DefinedModules.NamespaceId AS DefinedNamespaceId,
        DefinedNamespaces.Name   AS DefinedNamespaceName,
        DefinedNamespaces.StackId AS DefinedStackId,
        DefinedStacks.Name       AS DefinedStackName,
        CONCAT(DefinedStacks.Name, '/', DefinedNamespaces.Name, '/', DefinedModules.Name) AS DefinedDisplayName,

        mi.OutputModuleId        AS ReferencedModuleId,
        ReferencedModules.OrganizationId AS ReferencedOrganizationId,
        ReferencedModules.Name   AS ReferencedModuleName,
        ReferencedModules.NamespaceId AS ReferencedNamespaceId,
        ReferencedNamespaces.Name AS ReferencedNamespaceName,
        ReferencedNamespaces.StackId AS ReferencedStackId,
        ReferencedStacks.Name    AS ReferencedStackName,
        CONCAT(ReferencedStacks.Name, '/', ReferencedNamespaces.Name, '/', ReferencedModules.Name) AS ReferencedDisplayName

    FROM ModuleInputs mi
    INNER JOIN Modules DefinedModules ON mi.ModuleId = DefinedModules.Id
    INNER JOIN Namespaces DefinedNamespaces ON DefinedModules.NamespaceId = DefinedNamespaces.Id
    INNER JOIN Stacks DefinedStacks ON DefinedNamespaces.StackId = DefinedStacks.Id
    INNER JOIN Modules ReferencedModules ON mi.OutputModuleId = ReferencedModules.Id
    INNER JOIN Namespaces ReferencedNamespaces ON ReferencedModules.NamespaceId = ReferencedNamespaces.Id
    INNER JOIN Stacks ReferencedStacks ON ReferencedNamespaces.StackId = ReferencedStacks.Id
    WHERE mi.Discriminator IN ('ModuleEnvVarFromOutput', 'ModuleParamFromOutput', 'ModuleParamFromOutputSet');
