namespace SnapCd.Runner.Settings;

public class RunnerSettings
{
    public Guid OrganizationId { get; set; }

    public string Instance { get; set; } = string.Empty;

    public Guid Id { get; set; }

    public Credentials Credentials { get; set; } = new() { ClientId = string.Empty, ClientSecret = string.Empty };
}

public class Credentials
{
    public required string ClientId { get; set; }

    public required string ClientSecret { get; set; }
}