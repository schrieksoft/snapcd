// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Startup;

public static class TurnstileServices
{
    public static IServiceCollection AddTurnstileServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TurnstileSettings>(configuration.GetSection(TurnstileSettings.SectionName));

        services.AddHttpClient<TurnstileVerificationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
