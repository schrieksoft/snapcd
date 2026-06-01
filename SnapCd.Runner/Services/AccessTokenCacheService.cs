// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Caching.Memory;
using SnapCd.Runner.Constants;

namespace SnapCd.Runner.Services;

public class AccessTokenCacheService
{
    private readonly IMemoryCache _cache;

    public AccessTokenCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string? Get()
    {
        if (_cache.TryGetValue(MemoryCacheConstants.AccessTokenCacheKey, out string? accessToken)) return accessToken;

        return null;
    }
}