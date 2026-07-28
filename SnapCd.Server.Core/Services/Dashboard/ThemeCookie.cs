// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Services.Dashboard;

/// <summary>
/// Single source of truth for the theme cookie. The product is dark by default:
/// only an explicit "light" cookie opts out. Every server-side theme check must go
/// through this class; the inline scripts in the hosts' App.razor mirror the same
/// rule and must be kept in sync.
/// </summary>
public static class ThemeCookie
{
    public const string Name = "theme";
    public const string Dark = "dark";
    public const string Light = "light";

    public static bool IsDark(string? cookieValue) => cookieValue != Light;

    public static bool IsDark(HttpContext? context) => IsDark(context?.Request.Cookies[Name]);

    public static string ColorMode(bool isDark) => isDark ? Dark : Light;

    public static string ColorMode(HttpContext? context) => ColorMode(IsDark(context));

    /// <summary>Class for the &lt;html&gt; element, stamped server-side so the first paint has the right theme.</summary>
    public static string HtmlClass(HttpContext? context) => IsDark(context) ? "mud-dark" : "";
}
