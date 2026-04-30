namespace SnapCd.Server.Core.Misc.Exceptions;

public class QuotaExceededException : Exception
{
    public string EntityType { get; }
    public int CurrentCount { get; }
    public int Limit { get; }

    public QuotaExceededException(
        string entityType,
        int currentCount,
        int limit,
        string message) : base(message)
    {
        EntityType = entityType;
        CurrentCount = currentCount;
        Limit = limit;
    }

    public QuotaExceededException(
        string entityType,
        int currentCount,
        int limit,
        string message,
        Exception innerException) : base(message, innerException)
    {
        EntityType = entityType;
        CurrentCount = currentCount;
        Limit = limit;
    }
}
