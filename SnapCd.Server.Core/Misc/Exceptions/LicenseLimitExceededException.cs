namespace SnapCd.Server.Core.Misc.Exceptions;

public class LicenseLimitExceededException : Exception
{
    public string ResourceType { get; }
    public int CurrentUsage { get; }
    public int Limit { get; }
    public int RequestedCount { get; }
    public bool PayAsYouGoEnabled { get; }

    public LicenseLimitExceededException(
        string resourceType,
        int currentUsage,
        int limit,
        int requestedCount,
        string message) : base(message)
    {
        ResourceType = resourceType;
        CurrentUsage = currentUsage;
        Limit = limit;
        RequestedCount = requestedCount;
    }

    public LicenseLimitExceededException(
        string resourceType,
        int currentUsage,
        int limit,
        int requestedCount,
        string message,
        Exception innerException) : base(message, innerException)
    {
        ResourceType = resourceType;
        CurrentUsage = currentUsage;
        Limit = limit;
        RequestedCount = requestedCount;
    }
}