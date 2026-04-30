using System.Security.Cryptography;
using System.Text;
using MassTransit;

namespace SnapCd.Server.Core.Misc.Utils;

/// <summary>
/// A custom endpoint name formatter that truncates queue names to a specified length.
/// Uses kebab-case formatting with truncation and hash suffix to ensure uniqueness.
/// </summary>
public class TruncatedKebabCaseEndpointNameFormatter : IEndpointNameFormatter
{
    private const int HashSuffixLength = 8; // Length of hash suffix when truncation is needed
    private readonly KebabCaseEndpointNameFormatter _baseFormatter;
    private readonly int _maxQueueNameLength;

    public TruncatedKebabCaseEndpointNameFormatter(string prefix = "", bool includeNamespace = false, int maxQueueNameLength = 255)
    {
        _baseFormatter = new KebabCaseEndpointNameFormatter(prefix, includeNamespace);
        _maxQueueNameLength = maxQueueNameLength;
    }

    public string Separator => _baseFormatter.Separator;

    public string Consumer<T>() where T : class, IConsumer
    {
        return TruncateIfNeeded(_baseFormatter.Consumer<T>());
    }

    public string Message<T>() where T : class
    {
        return TruncateIfNeeded(_baseFormatter.Message<T>());
    }

    public string Saga<T>() where T : class, ISaga
    {
        return TruncateIfNeeded(_baseFormatter.Saga<T>());
    }

    public string ExecuteActivity<T, TArguments>() where T : class, IExecuteActivity<TArguments> where TArguments : class
    {
        return TruncateIfNeeded(_baseFormatter.ExecuteActivity<T, TArguments>());
    }

    public string CompensateActivity<T, TLog>() where T : class, ICompensateActivity<TLog> where TLog : class
    {
        return TruncateIfNeeded(_baseFormatter.CompensateActivity<T, TLog>());
    }

    public string TemporaryEndpoint(string tag)
    {
        return TruncateIfNeeded(_baseFormatter.TemporaryEndpoint(tag));
    }

    public string SanitizeName(string name)
    {
        return TruncateIfNeeded(_baseFormatter.SanitizeName(name));
    }

    private string TruncateIfNeeded(string queueName)
    {
        if (queueName.Length <= _maxQueueNameLength) return queueName;

        // Calculate available space for the original name (reserving space for separator and hash)
        var availableLength = _maxQueueNameLength - HashSuffixLength - 1; // -1 for separator

        // Truncate the original name and add a hash suffix for uniqueness
        var truncatedName = queueName[..availableLength];
        var hash = ComputeShortHash(queueName);

        return $"{truncatedName}-{hash}";
    }

    private static string ComputeShortHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

        // Take first 4 bytes and convert to lowercase hex (8 characters)
        return Convert.ToHexString(hash[..4]).ToLowerInvariant();
    }
}