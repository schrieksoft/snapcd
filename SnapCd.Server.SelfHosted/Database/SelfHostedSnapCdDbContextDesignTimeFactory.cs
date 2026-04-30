using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.SelfHosted.Database;

public class SelfHostedSnapCdDbContextDesignTimeFactory : IDesignTimeDbContextFactory<SelfHostedSnapCdDbContext>
{
    public SelfHostedSnapCdDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<SelfHostedSnapCdDbContext>();
        builder.UseSqlServer("Server=.;Database=SnapCd_DesignTime;Trusted_Connection=True;TrustServerCertificate=True;",
            m => m.MigrationsHistoryTable("__EFMigrationsHistory"));
        builder.UseOpenIddict();
        builder.UseOpenIddict<ServicePrincipal, Authorization, Scope, Token, Guid>();

        return new SelfHostedSnapCdDbContext(builder.Options);
    }
}
