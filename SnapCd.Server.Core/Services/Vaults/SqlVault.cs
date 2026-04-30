using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services.Vaults;

/// <summary>
/// SQL-backed <see cref="IVault"/> implementation. Stores AES-256-GCM ciphertext in the
/// <c>VaultSecrets</c> table (registered on the self-hosted context). <paramref name="vaultUrl"/>
/// passed in by the caller is ignored — the SQL store is one logical vault.
/// </summary>
public class SqlVault : IVault
{
    private const int NonceSize = 12; // AES-GCM nonce
    private const int TagSize = 16;   // AES-GCM tag

    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly byte[] _key;
    private readonly ILogger<SqlVault> _logger;

    public SqlVault(IDbContextFactory<SnapCdDbContext> dbContextFactory, byte[] key, ILogger<SqlVault> logger)
    {
        if (key.Length != 32)
            throw new ArgumentException("SqlVault symmetric key must be 32 bytes (AES-256).", nameof(key));

        _dbContextFactory = dbContextFactory;
        _key = key;
        _logger = logger;
    }

    public void Dispose() { }

    public async Task<SetIfChangedResult> SetIfChanged(string secretName, string value)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var set = db.Set<VaultSecret>();
        var existing = await set.FirstOrDefaultAsync(s => s.Name == secretName);

        if (existing != null && Decrypt(existing.Ciphertext) == value)
        {
            _logger.LogInformation("Secret \"{Name}\" unchanged; returning existing version.", secretName);
            return new SetIfChangedResult(existing.Version, WasChanged: false);
        }

        var version = await WriteAsync(db, set, existing, secretName, value);
        return new SetIfChangedResult(version, WasChanged: true);
    }

    public async Task<string> SetSecretAsync(string name, string value)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
            throw new ArgumentException("Secret name and value cannot be null or empty");

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var set = db.Set<VaultSecret>();
        var existing = await set.FirstOrDefaultAsync(s => s.Name == name);
        return await WriteAsync(db, set, existing, name, value);
    }

    public async Task<string> GetSecretAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Secret name cannot be null or empty", nameof(name));

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var row = await db.Set<VaultSecret>().FirstOrDefaultAsync(s => s.Name == name);
        if (row is null)
            throw new KeyNotFoundException($"Secret \"{name}\" not found.");

        return Decrypt(row.Ciphertext);
    }

    public async Task DeleteSecretAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Secret name cannot be null or empty", nameof(name));

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var affected = await db.Set<VaultSecret>()
            .Where(s => s.Name == name)
            .ExecuteDeleteAsync();

        if (affected == 0)
            _logger.LogInformation("Secret \"{Name}\" not found or already deleted.", name);
    }

    private async Task<string> WriteAsync(SnapCdDbContext db, DbSet<VaultSecret> set,
        VaultSecret? existing, string name, string value)
    {
        var ciphertext = Encrypt(value);
        var version = Guid.NewGuid().ToString("N");

        if (existing is null)
        {
            set.Add(new VaultSecret
            {
                Name = name,
                Ciphertext = ciphertext,
                Version = version
            });
        }
        else
        {
            existing.Ciphertext = ciphertext;
            existing.Version = version;
        }

        await db.SaveChangesAsync();
        return version;
    }

    private byte[] Encrypt(string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Layout: nonce || ciphertext || tag
        var blob = new byte[NonceSize + ciphertext.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, blob, 0, NonceSize);
        Buffer.BlockCopy(ciphertext, 0, blob, NonceSize, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, blob, NonceSize + ciphertext.Length, TagSize);
        return blob;
    }

    private string Decrypt(byte[] blob)
    {
        if (blob.Length < NonceSize + TagSize)
            throw new CryptographicException("Ciphertext blob is malformed.");

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var ciphertext = new byte[blob.Length - NonceSize - TagSize];
        Buffer.BlockCopy(blob, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(blob, NonceSize, ciphertext, 0, ciphertext.Length);
        Buffer.BlockCopy(blob, NonceSize + ciphertext.Length, tag, 0, TagSize);

        using var aes = new AesGcm(_key, TagSize);
        var plaintext = new byte[ciphertext.Length];
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }
}
