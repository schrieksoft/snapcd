using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.SelfHosted.Database.ClassMaps;

namespace SnapCd.Server.SelfHosted.Database;

public class SelfHostedSnapCdDbContext : SnapCdDbContext
{
    public SelfHostedSnapCdDbContext(DbContextOptions<SelfHostedSnapCdDbContext> options)
        : base(options)
    {
    }

    public DbSet<SelfHostedOrganizationLicense> SelfHostedOrganizationLicenses { get; set; } = null!;
    public DbSet<VaultSecret> VaultSecrets { get; set; } = null!;
    public DbSet<SecretMigrationAudit> SecretMigrationAudits { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new SelfHostedOrganizationLicenseClassMap());
        modelBuilder.ApplyConfiguration(new VaultSecretClassMap());
        modelBuilder.ApplyConfiguration(new SecretMigrationAuditClassMap());
    }
}
