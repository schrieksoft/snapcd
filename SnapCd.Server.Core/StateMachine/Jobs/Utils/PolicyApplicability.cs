// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using SnapCd.Contracts;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;

namespace SnapCd.Server.Core.StateMachine.Jobs.Utils;

public static class PolicyApplicability
{
    public static bool Matches(ResolvedPolicy policy, bool isDestroyJob)
    {
        return policy.EvaluateOn switch
        {
            PolicyEvaluateOn.ApplyOnly => !isDestroyJob,
            PolicyEvaluateOn.DestroyOnly => isDestroyJob,
            _ => true
        };
    }

    public static List<ResolvedPolicy> For(ResolvedModule declared, bool isDestroyJob)
    {
        return declared.Policies.Where(p => Matches(p, isDestroyJob)).ToList();
    }

    public static bool Any(string declaredJson, bool isDestroyJob)
    {
        var declared = JsonSerializer.Deserialize<ResolvedModule>(declaredJson);
        return declared != null && declared.Policies.Any(p => Matches(p, isDestroyJob));
    }
}
