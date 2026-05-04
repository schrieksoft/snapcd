using System.Buffers.Binary;
using System.Security.Cryptography;

namespace SnapCd.Server.Core.Misc.Utils;

public static class SecretHashingHelper
// NOTE, the below code was copied from the "ObfuscateClientSecretAsync" method (and the methods it depends on) as used in the OpenIddictApplicationManager class
{
    public static string ObfuscateClientSecret(string secret, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(secret)) throw new ArgumentException("Secret cannot be null or empty");

        var salt = CreateRandomArray(128);
        var hash = HashSecret(secret, salt, HashAlgorithmName.SHA256, 10_000, 256 / 8);

        return new string(Convert.ToBase64String(hash));
    }

    // public static bool ValidateClientSecret(string secret, string storedHash, string storedSalt,
    //     CancellationToken cancellationToken = default)
    // {
    //     var saltBytes = Convert.FromBase64String(storedSalt);
    //     var hashBytes =
    //         HashSecret(secret, saltBytes, HashAlgorithmName.SHA256, 10_000,
    //             256 / 8); // Hash the provided secret with the same salt
    //     var computedHash = Convert.ToBase64String(hashBytes);
    //
    //     // Compare the computed hash with the stored hash
    //     return storedHash == computedHash;
    // }

    private static byte[] CreateRandomArray(int size)
    {
        var algorithm = CryptoConfig.CreateFromName("OpenIddict RNG Cryptographic Provider") switch
        {
            RandomNumberGenerator result => result,
            null => null,
            var result => throw new CryptographicException(result.GetType().FullName)
        };

        if (algorithm is null) return RandomNumberGenerator.GetBytes(size / 8);

        var array = new byte[size / 8];

        try
        {
            algorithm.GetBytes(array);
        }

        finally
        {
            algorithm.Dispose();
        }

        return array;
    }

    private static byte[] HashSecret(string secret, byte[] salt, HashAlgorithmName algorithm, int iterations,
        int length)
    {
        var key = DeriveKey(secret, salt, algorithm, iterations, length);
        var payload = new byte[13 + salt.Length + key.Length];

        // Write the format marker.
        payload[0] = 0x01;

        // Write the hashing algorithm version.
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(1, sizeof(uint)), algorithm switch
        {
            var name when name == HashAlgorithmName.SHA1 => 0,
            var name when name == HashAlgorithmName.SHA256 => 1,
            var name when name == HashAlgorithmName.SHA512 => 2,

            _ => throw new InvalidOperationException()
        });

        // Write the iteration count of the algorithm.
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(5, sizeof(uint)), (uint)iterations);

        // Write the size of the salt.
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(9, sizeof(uint)), (uint)salt.Length);

        // Write the salt.
        salt.CopyTo(payload.AsSpan(13));

        // Write the subkey.
        key.CopyTo(payload.AsSpan(13 + salt.Length));

        return payload;
    }

    private static byte[] DeriveKey(string secret, byte[] salt, HashAlgorithmName algorithm, int iterations, int length)
    {
        // Warning: the type and order of the arguments specified here MUST exactly match the parameters used with
        // Rfc2898DeriveBytes(string password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm).
        using var generator =
            CryptoConfig.CreateFromName("OpenIddict PBKDF2 Cryptographic Provider", secret, salt, iterations,
                    algorithm) switch
                {
                    Rfc2898DeriveBytes result => result,
#pragma warning disable SYSLIB0060
                    null => new Rfc2898DeriveBytes(secret, salt, iterations, algorithm),
#pragma warning restore SYSLIB0060
                    var result => throw new CryptographicException(result.GetType().FullName)
                };

        return generator.GetBytes(length);
    }

    public static bool VerifyHashedSecret(string hash, string secret)
    {
        var payload = new ReadOnlySpan<byte>(Convert.FromBase64String(hash));
        if (payload.Length is 0) return false;

        // Verify the hashing format version.
        if (payload[0] is not 0x01) return false;

        // Read the hashing algorithm version.
        var algorithm = (int)BinaryPrimitives.ReadUInt32BigEndian(payload.Slice(1, sizeof(uint))) switch
        {
            0 => HashAlgorithmName.SHA1,
            1 => HashAlgorithmName.SHA256,
            2 => HashAlgorithmName.SHA512,

            _ => throw new InvalidOperationException()
        };

        // Read the iteration count of the algorithm.
        var iterations = (int)BinaryPrimitives.ReadUInt32BigEndian(payload.Slice(5, sizeof(uint)));

        // Read the size of the salt and ensure it's more than 128 bits.
        var saltLength = (int)BinaryPrimitives.ReadUInt32BigEndian(payload.Slice(9, sizeof(uint)));
        if (saltLength < 128 / 8) return false;

        // Read the salt.
        var salt = payload.Slice(13, saltLength);

        // Ensure the derived key length is more than 128 bits.
        var keyLength = payload.Length - 13 - salt.Length;
        if (keyLength < 128 / 8) return false;

        return FixedTimeEquals(
            payload.Slice(13 + salt.Length, keyLength),
            DeriveKey(secret, salt.ToArray(), algorithm, iterations, keyLength));
    }

    private static bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        if (left.Length != right.Length) return false;

        var length = left.Length;
        var accumulator = 0;

        for (var index = 0; index < length; index++) accumulator |= left[index] - right[index];

        return accumulator is 0;
    }
}