namespace SnapCd.Server.Core.Settings.DataSeeder.ToSeed;

public class UserToSeed
{
    public Guid? Id { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public bool IsSystemAdministrator { get; set; }
}
