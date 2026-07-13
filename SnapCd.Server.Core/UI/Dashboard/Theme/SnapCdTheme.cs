// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.

using MudBlazor;

namespace SnapCd.Server.Core.UI.Dashboard.Theme;

public static class SnapCdTheme
{
    private static readonly string[] FontFamily = ["Geist Sans", "system-ui", "-apple-system", "sans-serif"];

    private const string AccentColor = "#E85D1A";
    private const string AccentHover = "#DC5414";
    private const string AccentTint = "#FDEEE3";

    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#12171D",
            PrimaryDarken = "#0E1216",
            PrimaryLighten = "#2B333C",
            Secondary = AccentColor,
            SecondaryDarken = AccentHover,
            SecondaryLighten = AccentTint,
            SecondaryContrastText = "#FFFFFF",
            Tertiary = "#12171D",
            TertiaryDarken = "#0E1216",
            TertiaryLighten = "#2B333C",
            Info = "#4E8AF4",
            Success = "#00B368",
            Warning = "#D9A514",
            Error = "#DB4A3F",
            TextPrimary = "#12171D",
            TextSecondary = "#4A5561",
            TextDisabled = "#8B96A0",
            Background = "#FAFBFB",
            BackgroundGray = "#F2F4F6",
            Surface = "#FFFFFF",
            DrawerBackground = "#12171D",
            DrawerText = "#E8ECEF",
            DrawerIcon = "#E8ECEF",
            AppbarBackground = "#FAFBFB",
            AppbarText = "#12171D",
            LinesDefault = "#DDE3E8",
            LinesInputs = "#DDE3E8",
            TableLines = "#DDE3E8",
            TableStriped = "#F2F4F6",
            Divider = "#DDE3E8",
            DividerLight = "#F2F4F6"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#E8ECEF",
            PrimaryDarken = "#9AA5B0",
            PrimaryLighten = "#FFFFFF",
            Secondary = AccentColor,
            SecondaryDarken = AccentHover,
            SecondaryLighten = AccentTint,
            SecondaryContrastText = "#FFFFFF",
            Tertiary = "#12171D",
            TertiaryDarken = "#0E1216",
            TertiaryLighten = "#2B333C",
            Info = "#4E8AF4",
            Success = "#00B368",
            Warning = "#D9A514",
            Error = "#DB4A3F",
            Dark = "#000000",
            TextPrimary = "#E8ECEF",
            TextSecondary = "#9AA5B0",
            TextDisabled = "#6B7681",
            Background = "#000000",
            BackgroundGray = "#0E1216",
            Surface = "#171D24",
            DrawerBackground = "#0E1216",
            DrawerText = "#E8ECEF",
            DrawerIcon = "#E8ECEF",
            AppbarBackground = "#000000",
            AppbarText = "#E8ECEF",
            LinesDefault = "#262E37",
            LinesInputs = "#262E37",
            TableLines = "#262E37",
            TableStriped = "rgba(255,255,255,0.03)",
            Divider = "#262E37",
            DividerLight = "rgba(255,255,255,0.06)"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = FontFamily },
            H1 = new H1Typography { FontFamily = FontFamily },
            H2 = new H2Typography { FontFamily = FontFamily },
            H3 = new H3Typography { FontFamily = FontFamily },
            H4 = new H4Typography { FontFamily = FontFamily },
            H5 = new H5Typography { FontFamily = FontFamily },
            H6 = new H6Typography { FontFamily = FontFamily },
            Subtitle1 = new Subtitle1Typography { FontFamily = FontFamily },
            Subtitle2 = new Subtitle2Typography { FontFamily = FontFamily },
            Body1 = new Body1Typography { FontFamily = FontFamily },
            Body2 = new Body2Typography { FontFamily = FontFamily },
            Button = new ButtonTypography { FontFamily = FontFamily },
            Caption = new CaptionTypography { FontFamily = FontFamily },
            Overline = new OverlineTypography { FontFamily = FontFamily }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "0px"
        }
    };
}
