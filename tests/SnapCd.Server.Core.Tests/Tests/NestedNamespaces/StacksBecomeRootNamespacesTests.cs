// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.AgentSupplies;
using SnapCd.Server.Core.Entities.Definition.IntegrationEvents;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Host.Database;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// The statements the StacksBecomeRootNamespaces migration runs, against the seeded Stacks. Run
/// inside a transaction that is rolled back, so the shared database is left as it was.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class StacksBecomeRootNamespacesTests(Fixture fixture)
{
    private const string SqlResource =
        "SnapCd.Server.Host.Database.Migrations.Sql.20261128174321_StacksBecomeRootNamespaces.sql";

    [Fact]
    public async Task Each_stack_gets_a_root_namespace_that_holds_what_the_stack_held()
    {
        await using var db = fixture.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var org = fixture.Organizations["0"].Id;
        var stack = fixture.Stacks["00"];
        var runner = fixture.Runners.Values.First(r => r.OrganizationId == org);
        var user = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser;

        db.Namespaces.Add(new Namespace
        {
            Id = Guid.NewGuid(), Name = stack.Name, StackId = stack.Id, OrganizationId = org, CreatedDateTime = DateTime.UtcNow
        });
        db.Add(new RunnerStackSupply { Id = Guid.NewGuid(), OrganizationId = org, RunnerId = runner.Id, StackId = stack.Id });
        db.Add(new AgentStackSupply { Id = Guid.NewGuid(), OrganizationId = org, AgentId = fixture.Agents["0"].Id, StackId = stack.Id });
        db.Add(new StackIntegrationEvent
        {
            Id = Guid.NewGuid(), OrganizationId = org, IntegrationId = fixture.Integrations["Stack"].Id, StackId = stack.Id,
            Trigger = Enum.GetValues<IntegrationTrigger>()[0]
        });
        db.UserStackRoleAssignments.Add(new UserStackRoleAssignment
        {
            Id = Guid.NewGuid(), OrganizationId = org, StackId = stack.Id, UserId = user.Id, RoleName = StackRole.Contributor
        });
        db.UserStackRoleAssignments.Add(new UserStackRoleAssignment
        {
            Id = Guid.NewGuid(), OrganizationId = org, StackId = stack.Id, UserId = user.Id, RoleName = StackRole.NamespaceCreator
        });
        await db.SaveChangesAsync();

        var stacks = await Count(db, "dbo.Stacks");
        var stackRoles = await Count(db, "dbo.StackRoleAssignments");
        var contributors = await Count(db, "dbo.StackRoleAssignments WHERE RoleName = 'Contributor'");
        var stackSecrets = await Count(db, "dbo.Secrets WHERE Discriminator = 'StackSecret'");
        var namespaces = await Count(db, "dbo.Namespaces");

        Assert.True(stackSecrets > 0);
        Assert.Equal(0, await Count(db, "dbo.ModuleResolvedSettings r WHERE EXISTS (SELECT 1 FROM dbo.Stacks s WHERE s.Id = r.OutputRestrictionExemptionNamespaceId)"));

        // As the migration does: the name index is absent while the statements run and created after them.
        await db.Database.ExecuteSqlRawAsync(
            "DROP INDEX IX_Namespaces_OrganizationId_StackId_ParentNamespaceId_Name ON dbo.Namespaces");

        foreach (var batch in Batches())
            await db.Database.ExecuteSqlRawAsync(batch);

        await db.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IX_Namespaces_OrganizationId_StackId_ParentNamespaceId_Name " +
            "ON dbo.Namespaces (OrganizationId, StackId, ParentNamespaceId, Name)");

        Assert.Equal(namespaces + stacks, await Count(db, "dbo.Namespaces"));
        Assert.Equal(stacks, await Count(db,
            "dbo.Namespaces n JOIN dbo.Stacks s ON s.Id = n.Id AND s.Name = n.Name AND s.OrganizationId = n.OrganizationId " +
            "WHERE n.ParentNamespaceId IS NULL AND n.StackId = s.Id " +
            "AND n.DefaultOutputSetDownstreamRestriction = 'All' AND n.DefaultOutputRestrictionExemption = 'OwnNamespaceAndDescendants'"));

        Assert.Equal(0, await Count(db, "dbo.Namespaces WHERE ParentNamespaceId IS NULL AND Id <> StackId"));
        Assert.Equal(0, await Count(db,
            "dbo.Namespaces n WHERE NOT EXISTS (SELECT 1 FROM dbo.NamespaceAncestors a " +
            "WHERE a.NamespaceId = n.Id AND a.AncestorNamespaceId = n.StackId)"));

        Assert.Equal(stackRoles + contributors, await Count(db, "dbo.NamespaceRoleAssignments WHERE " + OnARoot("NamespaceId")));
        var given = await db.Database
            .SqlQueryRaw<string>(
                "SELECT RoleName AS Value FROM dbo.NamespaceRoleAssignments WHERE NamespaceId = {0} AND UserId = {1}", stack.Id, user.Id)
            .ToListAsync();
        Assert.Contains("Contributor", given);
        Assert.Contains("NestedNamespaceContributor", given);
        Assert.Contains("NestedNamespaceCreator", given);
        Assert.DoesNotContain("Owner", given);

        foreach (var (stackTable, namespaceTable) in new[]
                 {
                     ("RunnerStackSupplies", "RunnerNamespaceSupplies"),
                     ("AgentStackSupplies", "AgentNamespaceSupplies"),
                     ("IntegrationStackSupplies", "IntegrationNamespaceSupplies")
                 })
        {
            var held = await Count(db, $"dbo.{stackTable}");
            Assert.True(held > 0, stackTable);
            Assert.Equal(held, await Count(db, $"dbo.{namespaceTable} WHERE InheritedByDescendants = 1 AND " + OnARoot("NamespaceId")));
        }

        foreach (var (stackTable, namespaceTable) in new[]
                 {
                     ("StackMissions", "NamespaceMissions"),
                     ("StackIntegrationEvents", "NamespaceIntegrationEvents")
                 })
        {
            var held = await Count(db, $"dbo.{stackTable}");
            Assert.True(held > 0, stackTable);
            Assert.Equal(held, await Count(db, $"dbo.{namespaceTable} WHERE " + OnARoot("NamespaceId")));
        }

        Assert.Equal(0, await Count(db, "dbo.Secrets WHERE Discriminator = 'StackSecret' OR StackId IS NOT NULL"));
        Assert.Equal(stackSecrets, await Count(db,
            "dbo.Secrets WHERE Discriminator = 'NamespaceSecret' AND ScopeKind = 'Namespace' AND " + OnARoot("NamespaceId")));

        var secretId = fixture.StackSecrets.Values.First().Id;
        var converted = await db.NamespaceSecrets.AsNoTracking().SingleAsync(s => s.Id == secretId);
        Assert.Equal(fixture.StackSecrets.Values.First().StackId, converted.NamespaceId);

        Assert.True(await Count(db,
            "dbo.ModuleResolvedSettings r WHERE EXISTS (SELECT 1 FROM dbo.Stacks s WHERE s.Id = r.OutputRestrictionExemptionNamespaceId)") > 0);

        await transaction.RollbackAsync();
    }

    private static string OnARoot(string column) => $"EXISTS (SELECT 1 FROM dbo.Stacks s WHERE s.Id = {column})";

    private static Task<int> Count(SnapCdDbContext db, string fromAndWhere) =>
        db.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM {fromAndWhere}").SingleAsync();

    private static List<string> Batches()
    {
        using var stream = typeof(SelfHostedSnapCdDbContext).Assembly.GetManifestResourceStream(SqlResource)!;
        using var reader = new StreamReader(stream);

        return Regex.Split(reader.ReadToEnd(), @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Where(batch => !string.IsNullOrWhiteSpace(batch))
            .ToList();
    }
}
