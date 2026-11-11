// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text;
using System.Text.Json;
using Microsoft.Identity.Client.Extensions.Msal;

namespace SnapCd.Cli.Auth;

public sealed class StoredTokens
{
    public string? AccessToken { get; set; }
    public DateTimeOffset AccessTokenExpiresOn { get; set; }
    public string? RefreshToken { get; set; }
}

/// <summary>The tokens of every context, as one secret.</summary>
public interface ITokenStore
{
    /// <summary>True when the tokens are in a plaintext file because the platform has no secret store.</summary>
    bool IsUnprotected { get; }

    StoredTokens? Get(string context);
    void Set(string context, StoredTokens tokens);
    void Remove(string context);
}

/// <summary>
/// Keeps tokens in the platform's secret store: DPAPI on Windows, the Keychain on macOS, the
/// Secret Service on Linux. Where none is available, a file only the user can read.
/// </summary>
public class TokenStore : ITokenStore
{
    private const string FileName = "tokens.bin";
    private readonly Storage _storage;

    private TokenStore(StorageCreationProperties properties, bool isUnprotected)
    {
        _storage = Storage.Create(properties);
        IsUnprotected = isUnprotected;
        FilePath = properties.CacheFilePath;
    }

    public bool IsUnprotected { get; }

    private string FilePath { get; }

    public static TokenStore Open(string directory)
    {
        Directory.CreateDirectory(directory);

        var protectedProperties = new StorageCreationPropertiesBuilder(FileName, directory)
            .WithMacKeyChain("snapcd-cli", "tokens")
            .WithLinuxKeyring(
                "io.snapcd.cli",
                MsalCacheHelper.LinuxKeyRingDefaultCollection,
                "Snap CD CLI tokens",
                new KeyValuePair<string, string>("application", "snapcd-cli"),
                new KeyValuePair<string, string>("purpose", "tokens"))
            .Build();

        try
        {
            Storage.Create(protectedProperties).VerifyPersistence();
            return new TokenStore(protectedProperties, isUnprotected: false);
        }
        catch (MsalCachePersistenceException)
        {
            var unprotected = new StorageCreationPropertiesBuilder(FileName, directory)
                .WithUnprotectedFile()
                .Build();
            return new TokenStore(unprotected, isUnprotected: true);
        }
    }

    public StoredTokens? Get(string context) => ReadAll().GetValueOrDefault(context);

    public void Set(string context, StoredTokens tokens)
    {
        var all = ReadAll();
        all[context] = tokens;
        WriteAll(all);
    }

    public void Remove(string context)
    {
        var all = ReadAll();
        if (all.Remove(context))
            WriteAll(all);
    }

    private Dictionary<string, StoredTokens> ReadAll()
    {
        var data = _storage.ReadData();
        if (data is not { Length: > 0 })
            return new Dictionary<string, StoredTokens>(StringComparer.Ordinal);
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, StoredTokens>>(Encoding.UTF8.GetString(data))
                   ?? new Dictionary<string, StoredTokens>(StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new Dictionary<string, StoredTokens>(StringComparer.Ordinal);
        }
    }

    private void WriteAll(Dictionary<string, StoredTokens> all)
    {
        _storage.WriteData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(all)));
        if (IsUnprotected && !OperatingSystem.IsWindows() && File.Exists(FilePath))
            File.SetUnixFileMode(FilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}
