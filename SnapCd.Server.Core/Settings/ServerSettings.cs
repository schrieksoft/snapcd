namespace SnapCd.Server.Core.Settings;

public class ServerSettings
{
    public required string Host { get; set; }
    public Guid InstanceId { get; set; }
}