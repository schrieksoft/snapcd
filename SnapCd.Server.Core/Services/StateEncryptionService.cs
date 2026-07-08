// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services;

public interface IStateEncryptionService
{
    byte[] Encrypt(byte[] plaintext);
    byte[] Decrypt(byte[] ciphertext);
}

public class StateEncryptionService : IStateEncryptionService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public StateEncryptionService(IOptions<StateStoreSettings> options)
    {
        var keyBase64 = options.Value.EncryptionKey
            ?? throw new InvalidOperationException("StateStore:EncryptionKey is not configured.");
        _key = Convert.FromBase64String(keyBase64);
        if (_key.Length != 32)
            throw new InvalidOperationException("StateStore:EncryptionKey must be a base64-encoded 256-bit (32-byte) key.");
    }

    public byte[] Encrypt(byte[] plaintext)
    {
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var result = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);
        return result;
    }

    public byte[] Decrypt(byte[] ciphertextWithNonceAndTag)
    {
        if (ciphertextWithNonceAndTag.Length < NonceSize + TagSize)
            throw new CryptographicException("Ciphertext is too short.");

        var nonce = ciphertextWithNonceAndTag.AsSpan(0, NonceSize);
        var tag = ciphertextWithNonceAndTag.AsSpan(NonceSize, TagSize);
        var ciphertext = ciphertextWithNonceAndTag.AsSpan(NonceSize + TagSize);

        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }
}
