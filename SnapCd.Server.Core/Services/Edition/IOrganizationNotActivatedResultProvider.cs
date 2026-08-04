// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Mvc;

namespace SnapCd.Server.Core.Services.Edition;

/// <summary>
/// The API response returned when an organization is not activated. Null means the edition has
/// no such state and the request proceeds.
/// </summary>
public interface IOrganizationNotActivatedResultProvider
{
    IActionResult? CreateResult();
}

/// <summary>
/// For editions where organizations are always activated.
/// </summary>
public class NoOpOrganizationNotActivatedResultProvider : IOrganizationNotActivatedResultProvider
{
    public IActionResult? CreateResult() => null;
}
