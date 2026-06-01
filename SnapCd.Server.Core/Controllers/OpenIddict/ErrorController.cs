// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the creators participating to this project.
 */

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using SnapCd.Server.Core.Misc.ViewModels.Shared;

namespace SnapCd.Server.Core.Controllers.OpenIddict;

[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorController : Controller
{
    [HttpGet("error")]
    [HttpPost("error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // If the error was not caused by an invalid
        // OIDC request, display a generic error page.
        var response = HttpContext.GetOpenIddictServerResponse();
        if (response == null) return View(new ErrorViewModel());

        return View(new ErrorViewModel
        {
            Error = response.Error ?? string.Empty,
            ErrorDescription = response.ErrorDescription ?? string.Empty
        });
    }
}