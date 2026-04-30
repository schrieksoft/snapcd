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
        await _jsRuntime.InvokeVoidAsync("setThemeCookie", _isDarkMode ? "dark" : "light");
        OnThemeChanged?.Invoke();
    }

    public bool IsDarkMode => _isDarkMode;
}