using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnapCd.Server.Host.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleAndNamespaceHookEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IgnoreNamespaceHooks",
                table: "Modules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ModuleHooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Task = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Phase = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Script = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleHooks_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleHooks_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NamespaceHooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Task = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Phase = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Script = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NamespaceHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NamespaceHooks_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceHooks_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleHooks_ModuleId_OrganizationId",
                table: "ModuleHooks",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleHooks_ModuleId_Task_Phase",
                table: "ModuleHooks",
                columns: new[] { "ModuleId", "Task", "Phase" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleHooks_OrganizationId",
                table: "ModuleHooks",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceHooks_NamespaceId_OrganizationId",
                table: "NamespaceHooks",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceHooks_NamespaceId_Task_Phase",
                table: "NamespaceHooks",
                columns: new[] { "NamespaceId", "Task", "Phase" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceHooks_OrganizationId",
                table: "NamespaceHooks",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModuleHooks");

            migrationBuilder.DropTable(
                name: "NamespaceHooks");

            migrationBuilder.DropColumn(
                name: "IgnoreNamespaceHooks",
                table: "Modules");
        }
    }
}
