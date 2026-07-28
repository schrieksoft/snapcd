// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.JSInterop;

namespace SnapCd.Server.Core.Services.Dashboard;

public class ThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private bool _isDarkMode;
    private bool _isInitialized;

    public event Action? OnThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public void Initialize(bool isDarkMode)
    {
        if (_isInitialized) return;
        _isDarkMode = isDarkMode;
        _isInitialized = true;
    }

    public async Task ToggleThemeAsync()
    {
        _isDarkMode = !_isDarkMode;
        await _jsRuntime.InvokeVoidAsync("setThemeCookie", ThemeCookie.ColorMode(_isDarkMode));
        OnThemeChanged?.Invoke();
    }

    public bool IsDarkMode => _isDarkMode;
}