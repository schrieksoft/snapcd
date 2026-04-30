namespace SnapCd.Server.Core.Settings.DataSeeder;

public class ProductionDataSeederSettings
{
    public PreseededSettings Preseeded { get; set; } = new();
}

public class PreseededSettings
{
    public static readonly Guid DefaultId = new("10000000-0000-0000-0000-000000000000");

    public bool Enabled { get; set; } = false;

    public OrganizationPreseed Organization { get; set; } = new();
    public UserPreseed User { get; set; } = new();
    public RunnerPreseed Runner { get; set; } = new();
    public StackPreseed Stack { get; set; } = new();
}

public class OrganizationPreseed
{
    public Guid? Id { get; set; } = PreseededSettings.DefaultId;
    public string Name { get; set; } = "default";
}

public class UserPreseed
{
    public Guid? Id { get; set; } = PreseededSettings.DefaultId;
    public string Email { get; set; } = "admin@preseeded.io";
    public string Password { get; set; } = "Admin#123";
    public bool IsSystemAdministrator { get; set; } = true;
}

public class RunnerPreseed
{
    public Guid? Id { get; set; } = PreseededSettings.DefaultId;
    public string Name { get; set; } = "default";
    public Guid? ServicePrincipalId { get; set; } = PreseededSettings.DefaultId;
    public string ServicePrincipalClientId { get; set; } = "default";
    public string ServicePrincipalClientSecret { get; set; } = "default";
}

public class StackPreseed
{
    public Guid? Id { get; set; } = PreseededSettings.DefaultId;
    public string Name { get; set; } = "default";
    public Guid? SampleSecretId { get; set; } = PreseededSettings.DefaultId;
    public string SampleSecretName { get; set; } = "sample";
    public string SampleSecretValue { get; set; } = "sample";
}

public class UserToPreseed
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = null!;
    public string Password { get; set; } = null!;
    public Guid OrganizationId { get; set; }
}
