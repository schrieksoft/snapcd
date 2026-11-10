// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace SnapCd.Server.Core.Services.Nonsecured.Federation;

/// <summary>
/// Active only for a request that carries no federated assertion. The handlers requiring a
/// confidential client to present a client secret are filtered on this.
/// </summary>
public class RequireNoFederatedAssertion : IOpenIddictServerHandlerFilter<ProcessAuthenticationContext>
{
    public ValueTask<bool> IsActiveAsync(ProcessAuthenticationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new ValueTask<bool>(!FederationParameters.CarriesAssertion(context.Request));
    }
}

/// <summary>The same filter for the token-request validation stage.</summary>
public class RequireNoFederatedAssertionOnTokenRequest : IOpenIddictServerHandlerFilter<ValidateTokenRequestContext>
{
    public ValueTask<bool> IsActiveAsync(ValidateTokenRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new ValueTask<bool>(!FederationParameters.CarriesAssertion(context.Request));
    }
}
