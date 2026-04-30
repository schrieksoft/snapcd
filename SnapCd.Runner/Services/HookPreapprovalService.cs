using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SnapCd.Runner.Exceptions;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Services;

/// <summary>
/// Service that validates incoming hooks against pre-approved hooks stored in a directory.
/// When enabled, all hooks must match (by SHA256 hash) a pre-approved hook file.
/// </summary>
public class HookPreapprovalService
{
    private readonly ILogger<HookPreapprovalService> _logger;
    private readonly HooksPreapprovalSettings _settings;
    private readonly HashSet<string> _preapprovedHookHashes;

    public HookPreapprovalService(
        ILogger<HookPreapprovalService> logger,
        IOptions<HooksPreapprovalSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        _preapprovedHookHashes = new HashSet<string>();

        if (_settings.Enabled)
        {
            LoadPreapprovedHooks();
        }
        else
        {
            _logger.LogInformation("Hook pre-approval validation is disabled");
        }
    }

    /// <summary>
    /// Validates a single hook against pre-approved hooks.
    /// </summary>
    /// <param name="hookContent">The hook script content to validate</param>
    /// <param name="hookName">The name of the hook (for error messages)</param>
    /// <exception cref="HookNotPreapprovedException">Thrown when validation is enabled and hook is not pre-approved</exception>
    public void ValidateHook(string? hookContent, string hookName)
    {
        // If validation is disabled, allow all hooks
        if (!_settings.Enabled)
        {
            return;
        }

        // Empty or null hooks are allowed (means no hook to execute)
        if (string.IsNullOrWhiteSpace(hookContent))
        {
            return;
        }

        // Compute hash of incoming hook
        var hookHash = ComputeHash(hookContent);

        // Check if hash matches any pre-approved hook
        if (!_preapprovedHookHashes.Contains(hookHash))
        {
            _logger.LogWarning(
                "Hook '{HookName}' failed pre-approval validation. Hash: {Hash}",
                hookName, hookHash);
            throw new HookNotPreapprovedException(hookName, hookContent);
        }

        _logger.LogDebug("Hook '{HookName}' passed pre-approval validation", hookName);
    }

    /// <summary>
    /// Validates multiple hooks against pre-approved hooks.
    /// </summary>
    /// <param name="hooks">Array of tuples containing (hookContent, hookName)</param>
    /// <exception cref="HookNotPreapprovedException">Thrown when validation is enabled and any hook is not pre-approved</exception>
    public void ValidateHooks(params (string? content, string name)[] hooks)
    {
        foreach (var (content, name) in hooks)
        {
            ValidateHook(content, name);
        }
    }

    private void LoadPreapprovedHooks()
    {
        var directory = _settings.PreapprovedHooksDirectory;

        if (string.IsNullOrWhiteSpace(directory))
        {
            _logger.LogWarning(
                "Hook pre-approval is enabled but PreapprovedHooksDirectory is not set. " +
                "All non-empty hooks will be rejected.");
            return;
        }

        if (!Directory.Exists(directory))
        {
            _logger.LogWarning(
                "Hook pre-approval is enabled but directory '{Directory}' does not exist. " +
                "All non-empty hooks will be rejected.",
                directory);
            return;
        }

        var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            try
            {
                var content = File.ReadAllText(file);
                var hash = ComputeHash(content);
                _preapprovedHookHashes.Add(hash);

                _logger.LogDebug(
                    "Loaded pre-approved hook from '{File}' with hash {Hash}",
                    file, hash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to load pre-approved hook from file '{File}'",
                    file);
            }
        }

        _logger.LogInformation(
            "Loaded {Count} pre-approved hooks from '{Directory}'",
            _preapprovedHookHashes.Count, directory);
    }

    private static string ComputeHash(string content)
    {
        // Normalize line endings to Unix (LF) to avoid platform differences
        var normalized = content.Replace("\r\n", "\n");

        // Trim trailing whitespace to be forgiving of minor formatting differences
        normalized = normalized.TrimEnd();

        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
