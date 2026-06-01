// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Misc.Constants;

public class ClaimTypeConstants
{
    public const string
        SubjectClaimType =
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"; //JwtRegisteredClaimNames.Sub;

    public const string PrincipalDiscriminatorClaimType = "principal_discriminator";

    public const string OrganizationClaimType = "organizations";

    public const string
        NameClaimType =
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
}