# Permission tests

> How the tests in this directory are organized, what each piece is responsible for, and what
> to do when expanding coverage.

## TL;DR

```
Permissions/
├── Smoke/             — one positive + one negative test per (entity, action). Proves the
│                        secured-repo binding works for the most common role/action paths.
├── RoleResolution/    — one test per declared role family in a secured-repo PermissionMap.
│                        Proves each family's join is wired in the underlying RoleQuery.
└── PrincipalSource/   — four tests, one per principal source (User / SP / GroupMember /
                         NestedGroupMember). Proves the dispatch machinery treats sources
                         identically.
```

Run them all with `dotnet test --filter "FullyQualifiedName~Tests.Permissions"`.

## The three tiers

The redesign collapses three orthogonal axes that the original test layout multiplied together:

| Axis | Distinct values | Tested in |
|---|---|---|
| Role family | OrganizationRole / StackRole / NamespaceRole / ModuleRole / AgentRole / RunnerRole | Tier B (per-family wiring) |
| Principal source | User / ServicePrincipal / GroupMember / NestedGroupMember | Tier C (principal dispatch) |
| Entity family | concrete repos derived from `Generic*ChildSecuredRepository` | Tier A (per-entity smoke) |

Each tier is tested **once** in its own focused folder. None of the tiers should multiply
across each other.

### Tier A — Smoke (`Smoke/`)

**Purpose.** For each distinct secured-repo handler shape, prove the basic binding works:
*the right principal can perform the action; the wrong principal cannot.*

**Shape per file.** One file per representative entity. Inside each file: 5 actions × 2 cases
= 10 `[Fact]` methods.

| Action | Positive | Negative |
|---|---|---|
| Get | OrganizationRole.Owner returns the row | NoPermissionUser → `PrincipalNotAuthorizedException` |
| List | Owner sees pre-seeded rows | NoPermissionUser sees empty list |
| Create | Owner allocates a new entity in the test method and `Create` succeeds | NoPermissionUser → `PrincipalNotAuthorizedException` |
| Update | Owner mutates a **dedicated pre-seeded** entity (`SmokeXxx["{TestClassName}_UpdateCan"]`) | NoPermissionUser → `PrincipalNotAuthorizedException` |
| Delete | Owner deletes a **dedicated pre-seeded** entity (`SmokeXxx["{TestClassName}_DeleteCan"]`) | NoPermissionUser → `PrincipalNotAuthorizedException` |

**The dedicated-entity rule is load-bearing.** Update and Delete mutate the row. To avoid
test interference and to keep tests independent of order, each smoke class gets its own pair of
rows seeded in `Fixture.CreateSmokeTestEntities`. Create allocates in the test (no pre-seed
needed — the row didn't exist before the test).

**Which entities get a file.** One representative entity per `Generic*ChildSecuredRepository`
base, plus one per bespoke override on a `*Query` / `Can*` / scope-spanning `PermissionMap`:

- Generic-base reps: `Stack`, `Namespace`, `Module`, `ModuleHook`, `AgentModuleAssignment`,
  `RunnerModuleAssignment`.
- Bespoke override files get added when an entity overrides Query/Can methods *beyond just
  declaring its PermissionMap*. (See the existing `Smoke/` files — at time of writing, the
  generic-base reps cover the smoke surface; add bespoke files as new override points appear.)

### Tier B — Per-family wiring (`RoleResolution/`)

**Purpose.** Each secured-repo `PermissionMap` declares roles from multiple families:

```csharp
new PermissionMap
{
    OrganizationRoles = [...],
    StackRoles        = [...],
    NamespaceRoles    = [...],
    ModuleRoles       = [...],
    AgentRoles        = [...],
    RunnerRoles       = [...],
}
```

Any family entry can be **declaratively present but functionally inert** if the corresponding
join in the generic base's `RoleQuery` isn't wired. Tier A wouldn't catch this — it only
exercises one family (typically `OrganizationRoles`). Tier B proves that each declared family's
join actually does something.

**Shape per test.** Seed a principal with the family's minimum sufficient role on the
appropriate scope row. Call `repo.List(orgId)`. Assert the result is **non-empty**. Non-empty
proves the join is connected. Scope-exclusion correctness is *not* asserted here — that's Tier
A's negative test (`NoPermissionUser` → empty result).

**Which families get a test.** For each generic base, every family declared in PermissionMaps
beyond `OrganizationRoles`:

| File | Generic base | Tests |
|---|---|---|
| `ScopeChain_RoleResolutionTests.cs` | `GenericStackChild`, `GenericNamespaceChild`, `GenericModuleChild` | StackRoles wiring at each level; NamespaceRoles at `GenericNamespaceChild`+`GenericModuleChild`; ModuleRoles at `GenericModuleChild`. 6 tests total. |
| `AgentChain_RoleResolutionTests.cs` | `GenericAgentChild` | AgentRoles wiring. 1 test. |
| `RunnerChain_RoleResolutionTests.cs` | `GenericRunnerChild` | RunnerRoles wiring. 1 test. |
| `MissionCrossScope_RoleResolutionTests.cs` | bespoke `*MissionSecuredRepository` ReadQuery overrides (Phase 16.7.1) | Agent-side wiring per mission scope. 4 tests. |

**Tier B does NOT test:**
- The specific row set that a role sees (Tier A's job, via Smoke's positive case).
- Scope-exclusion ("StackRole.Reader on Stack0 doesn't see Stack1's rows") — Tier A's negative
  test covers "wrong principal sees nothing"; sibling-scope exclusion follows by construction.
- Cross-role substitutability — by definition, if each declared family is independently wired,
  Tier A's choice of "test only OrganizationRoles for this entity" suffices.

**Tier B currently covers ReadQuery only.** `CreateQuery` / `UpdateQuery` / `DeleteQuery`
wiring is exposed to the same risk, but they have independent join implementations. If
read-wiring is clean, write-side wiring is highly likely to be clean too (same author, same
pattern). Add Create/Update/Delete wiring tests if a bug surfaces — they'd follow the same
shape.

### Tier C — Principal-source dispatch (`PrincipalSource/`)

**Purpose.** Prove that the principal-resolution machinery treats all four sources identically
when they hold the same role on the same scope. Run once on the simplest representative entity
(`Stack`).

**Shape.** Five tests, all asserting `CanGet` on Stack00:
- `User_DirectRoleAssignment_CanReadStack0`
- `ServicePrincipal_DirectRoleAssignment_CanReadStack0`
- `User_ViaGroupMembership_CanReadStack0`
- `User_ViaNestedGroupMembership_CanReadStack0`
- `User_NoRoleAnywhere_CannotReadStack0` (control case)

All four positive principals hold `OrganizationRole.Owner` on Org0 via different mechanisms.
All must succeed. If they share a code path (the `RoleQuery` joins all four assignment tables),
passing once on Stack is enough — the dispatch is shared infrastructure across every secured
repo.

## How the fixture supports the tests

`Tests/Infrastructure/Fixture.cs` is the shared seed. It runs once per xUnit test session
(`ICollectionFixture<Fixture>` via `[Collection("NewRoleBasedSharedFixture")]`).

What it seeds:

- **Two organizations**: `Organizations["0"]` (full hierarchy) and `Organizations["1"]`
  (cross-org isolation slice).
- **Hierarchy in Org0**: binary-tree of Stacks / Namespaces / Modules keyed by path (`00`, `000`,
  `0000`, etc). Plus per-scope `Secrets`, `Inputs`, `Outputs`, `OutputSets`, `Variables`,
  `VariableSets`, `ModuleHooks`, `ModuleJobs`.
- **Principals per OrganizationRole** (every enum value): direct User, direct ServicePrincipal,
  GroupMember, NestedGroupMember. Lookup via `OrganizationPrincipals["0"][OrganizationRole.X]`.
- **Principals per RunnerRole**: same shape, `RunnerPrincipals["0"][RunnerRole.X]`.
- **Tier B scope-role Reader Users**: keyed by `ScopeReaderUsers["Stack00.Reader"]`,
  `["Namespace000.Reader"]`, `["Module0000.Reader"]`, `["Agent0.Reader"]`, etc.
- **Agents + Agent assignments + Runner sibling**: `Agents["0"]`, `Agents["0Sibling"]`,
  `Agents["1"]`, `AgentModuleAssignments["0"]` + `["0Sibling"]`, `Runners["0Sibling"]`.
- **Missions** (one per scope, owned by `Agents["0"]`): `OrganizationMissions["0"]`,
  `StackMissions["0"]`, `NamespaceMissions["0"]`, `ModuleMissions["0"]`, plus
  `ModuleMissions["0Sibling"]` (owned by `Agents["0Sibling"]`).
- **Tier A dedicated entities for Update/Delete**: `SmokeStacks` / `SmokeNamespaces` /
  `SmokeModules` / `SmokeModuleHooks` / `SmokeAgentAssignments` / `SmokeRunnerAssignments`,
  each keyed by `"{TestClassName}_UpdateCan"` and `"{TestClassName}_DeleteCan"`.
- **`NoPermissionUser`** and **`NoPermissionServicePrincipal`** for negative cases.
- **`UserOrganizationRoleAssignments["Org1Reader"]`** for cross-org negatives.

## Expanding the tests

### Adding a new entity that uses an existing generic base with no overrides

If the new entity is just a thin derivation of an existing `Generic*ChildSecuredRepository`
with no `*Query` / `Can*` / `PermissionMap` overrides, **no new test file is needed**. The
generic base's behaviour is already covered by the representative entity for that base. The
new entity rides along.

If you want belt-and-braces coverage, add a smoke file mirroring the existing patterns — but
it should never be necessary.

### Adding a new entity that overrides `*Query` / `Can*` / `PermissionMap`

1. **Add a Tier A smoke file** at `Smoke/{Entity}_SmokeTests.cs`. Use one of the existing
   files as a template. Each smoke file has:
   - A class with `[Collection("NewRoleBasedSharedFixture")]`.
   - A `Repo(Guid principalId)` helper that constructs the secured repository.
   - 10 `[Fact]` methods covering Get/List/Create/Update/Delete × positive/negative.
   - Update and Delete positive tests use `_fixture.SmokeXxx["{TestClassName}_UpdateCan"]` /
     `_DeleteCan` for their target row.

2. **Add the dedicated Update/Delete entities to the fixture** — open `Fixture.cs`, find
   `CreateSmokeTestEntities`, and add the two new rows there. Add a new collection if the
   entity type isn't already represented (e.g., `SmokeXxx` Dictionary).

3. **If the new entity's PermissionMap declares a role family that no other entity exposes**
   (e.g., you add a new role enum), also add a Tier B test in the appropriate
   `RoleResolution/*.cs` file. See "Adding a new role family" below.

### Adding a new role family

If you introduce a new role enum (e.g., `OutputRole`):

1. Update `Fixture.cs` to seed at least one principal per family member you'll test against
   (typically just `Reader`). Mirror the pattern in `CreateScopeReaderPrincipals_Org0`.
2. Add the new principal's key to `ScopeReaderUsers` (or a new dictionary).
3. Add a Tier B wiring test in the appropriate file (or create a new `*Chain_RoleResolutionTests.cs`
   if the family belongs to a new scope chain).
4. Update each secured repo's `PermissionMap` to declare the family where appropriate. Tier A
   already exercises `OrganizationRoles`; Tier B will exercise the new family.

### Adding a new generic base (`Generic{Scope}ChildSecuredRepository`)

This is rare. When it happens:

1. Pick the simplest derived entity as the smoke representative; add `Smoke/{Entity}_SmokeTests.cs`.
2. Add a new `RoleResolution/{Scope}Chain_RoleResolutionTests.cs` covering each family the
   base declares.
3. Update the fixture to seed any principals / entities the new chain needs.
4. Update this README's "Which families get a test" table.

### Adding a new bespoke ReadQuery override

If a repo adds a non-PermissionMap path (a custom `ReadQuery` join that introduces a new
visibility source — like the mission entities' agent-side path), add a test to the relevant
`RoleResolution/*.cs` file. The shape: seed a principal that should be visible via the new path
and assert non-empty.

## What these tests do NOT cover

- **Controller-layer authorization** (`[Authorize]` attribute behaviour, JWT scope validation,
  OpenIddict integration). Tested separately (or not yet — see `ai-agent-plan.md` Phase 24.10).
- **UI-layer gating** (`AuthorizeOnNavigationPermission` etc).
- **MCP tool permission gating** — covered by the MCP gen's own tests.
- **`CreateQuery` / `UpdateQuery` / `DeleteQuery` wiring at the family level** — currently
  only `ReadQuery` wiring is exercised in Tier B. Risk noted; deferred.
- **Performance / SQL-shape regressions** — `RoleQuery` join shape changes don't show up here
  unless they break correctness.

## Running

```bash
# Whole tree
dotnet test tests/SnapCd.Server.Core.Tests/SnapCd.Server.Core.Tests.csproj \
  --filter "FullyQualifiedName~Tests.Permissions"

# Just one tier
dotnet test --filter "FullyQualifiedName~Tests.Permissions.Smoke"
dotnet test --filter "FullyQualifiedName~Tests.Permissions.RoleResolution"
dotnet test --filter "FullyQualifiedName~Tests.Permissions.PrincipalSource"

# Single file
dotnet test --filter "FullyQualifiedName~Stack_SmokeTests"
```

The first run starts a SQL Server container via Testcontainers (~10s warm-up). Subsequent runs
in the same session reuse the same container.
