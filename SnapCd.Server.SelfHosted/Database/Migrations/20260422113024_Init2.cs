using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnapCd.Server.SelfHosted.Database.Migrations
{
    /// <inheritdoc />
    public partial class Init2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PublicKeyFetchedAtUtc",
                table: "SelfHostedOrganizationLicenses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicKeyPem",
                table: "SelfHostedOrganizationLicenses",
                type: "nvarchar(max)",
                maxLength: 4096,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicKeyFetchedAtUtc",
                table: "SelfHostedOrganizationLicenses");

            migrationBuilder.DropColumn(
                name: "PublicKeyPem",
                table: "SelfHostedOrganizationLicenses");
        }
    }
}
