// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Validation;

public class CachingSettingsValidator : IValidateOptions<CachingSettings>
{
    public ValidateOptionsResult Validate(string? name, CachingSettings options)
    {
        if (options.Provider == CacheProvider.Redis && string.IsNullOrEmpty(options.ConnectionString))
            return ValidateOptionsResult.Fail("Caching:ConnectionString is required when Caching:Provider is 'Redis'.");

        return ValidateOptionsResult.Success;
    }
}
