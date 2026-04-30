using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnapCd.Server.SelfHosted.Database.Migrations
{
    /// <inheritdoc />
    public partial class Init4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DryRun",
                table: "SecretMigrationAudits");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DryRun",
                table: "SecretMigrationAudits",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
