// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using OpenIddict.Abstractions;

namespace SnapCd.Server.Core.Services.Nonsecured.Federation;

/// <summary>
/// Token request parameters that carry a workload's own platform token, so it can authenticate as
/// a Service Principal without a client secret.
/// </summary>
public static class FederationParameters
{
    /// <summary>
    /// The presented token. Deliberately not the standard `client_assertion`: that name is
    /// handled by OpenIddict's own private_key_jwt support, which requires the assertion to be
    /// issued by the client itself and signed with a key registered against it. Here the issuer
    /// is a separate platform and the key is the platform's.
    /// </summary>
    public const string ClientAssertion = "snapcd_federated_assertion";

    public const string ClientAssertionType = "snapcd_federated_assertion_type";

    public const string JwtBearerClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";

    /// <summary>Whether a request presents a federated assertion.</summary>
    public static bool CarriesAssertion(OpenIddictRequest? request)
        => !string.IsNullOrEmpty((string?)request?.GetParameter(ClientAssertion));
}
