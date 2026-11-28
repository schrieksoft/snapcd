// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Services.Interfaces.Vaults;
using SnapCd.Server.Core.Services.Utils.Vaults;

namespace SnapCd.Server.Core.Tests.Tests.Vaults;

/// <summary>
/// A Namespace Secret that began as a Stack Secret has its value under the Stack name until it is
/// first read.
/// </summary>
public class SecretValueReaderTests
{
    private static readonly Guid Org = Guid.NewGuid();

    private readonly NamespaceSecret _secret = new()
    {
        Id = Guid.NewGuid(), NamespaceId = Guid.NewGuid(), OrganizationId = Org, Name = "s"
    };

    private string OwnName => RemoteSecretName.For(_secret, Org);
    private string StackName => $"stack--{Org}--{_secret.NamespaceId}--{_secret.Id}";

    [Fact]
    public async Task A_value_under_the_stack_name_is_returned_and_written_under_the_namespace_name()
    {
        var vault = new FakeVault { Values = { [StackName] = "from-stack" } };

        Assert.Equal("from-stack", await SecretValueReader.Read(vault, _secret, Org));
        Assert.Equal("from-stack", vault.Values[OwnName]);
        Assert.Equal("from-stack", vault.Values[StackName]);
    }

    [Fact]
    public async Task A_value_under_the_namespace_name_wins_over_one_left_under_the_stack_name()
    {
        var vault = new FakeVault { Values = { [StackName] = "old", [OwnName] = "new" } };

        Assert.Equal("new", await SecretValueReader.Read(vault, _secret, Org));
        Assert.Equal("old", vault.Values[StackName]);
    }

    [Fact]
    public async Task A_secret_not_yet_stored_under_its_owner_is_found_under_the_shorter_stack_name()
    {
        _secret.NameContainsScope = false;
        var vault = new FakeVault { Values = { [$"stack--{Org}--{_secret.Id}"] = "from-stack" } };

        Assert.Equal("from-stack", await SecretValueReader.Read(vault, _secret, Org));
        Assert.Equal("from-stack", vault.Values[$"namespace--{Org}--{_secret.Id}"]);
    }

    [Fact]
    public async Task A_value_under_neither_name_fails_as_a_plain_miss()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => SecretValueReader.Read(new FakeVault(), _secret, Org));
    }

    [Fact]
    public async Task A_vault_that_fails_to_answer_is_not_read_as_a_miss()
    {
        var vault = new FakeVault { Values = { [StackName] = "old" }, FailOn = OwnName };

        await Assert.ThrowsAsync<TimeoutException>(() => SecretValueReader.Read(vault, _secret, Org));
        Assert.False(vault.Values.ContainsKey(OwnName));
    }

    [Fact]
    public async Task A_module_secret_is_never_looked_for_under_a_stack_name()
    {
        var secret = new ModuleSecret { Id = _secret.Id, ModuleId = _secret.NamespaceId, OrganizationId = Org, Name = "s" };
        var vault = new FakeVault { Values = { [StackName] = "from-stack" } };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => SecretValueReader.Read(vault, secret, Org));
    }

    [Fact]
    public async Task Deleting_removes_the_value_under_both_names()
    {
        var vault = new FakeVault { Values = { [StackName] = "old", [OwnName] = "new" } };

        await SecretValueReader.Delete(vault, _secret, Org);

        Assert.Empty(vault.Values);
    }

    private sealed class FakeVault : IVault
    {
        public Dictionary<string, string> Values { get; } = [];
        public string? FailOn { get; init; }

        public Task<string> GetSecretAsync(string secretName)
        {
            if (secretName == FailOn) throw new TimeoutException();
            return Values.TryGetValue(secretName, out var value)
                ? Task.FromResult(value)
                : throw new KeyNotFoundException(secretName);
        }

        public async Task<VersionedSecret> GetSecretWithVersionAsync(string secretName) =>
            new(await GetSecretAsync(secretName), "1");

        public Task<string> SetSecretAsync(string secretName, string value)
        {
            Values[secretName] = value;
            return Task.FromResult("1");
        }

        public Task DeleteSecretAsync(string secretName)
        {
            Values.Remove(secretName);
            return Task.CompletedTask;
        }

        public Task<SetIfChangedResult> SetIfChanged(string secretName, string value) => throw new NotSupportedException();
        public Task<bool> IsSoftDeletedAsync(string secretName, CancellationToken ct = default) => Task.FromResult(false);
        public Task<IReadOnlyList<string>> ListNamesAsync(string prefix, CancellationToken ct = default) => throw new NotSupportedException();
        public void Dispose() { }
    }
}
