namespace SnapCd.Server.Core.Misc.Helpers;

public static class MassTransitHelpers
{
    public static string GetConsumerEndpoint(Guid serverInstanceId, string messageTypeName)
    {
        return $"queue:runner--{serverInstanceId.ToString("N")}--{messageTypeName.ToLower()}";
    }
}