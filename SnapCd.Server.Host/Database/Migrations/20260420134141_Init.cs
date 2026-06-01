// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnapCd.Server.Host.Database.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModuleModifiedSaga",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentState = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimeoutTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleModifiedSaga", x => new { x.CorrelationId, x.OrganizationId });
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(127)", maxLength: 127, nullable: false),
                    InputKeyVaultUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    OutputKeyVaultUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeletedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Scopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConcurrencyToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descriptions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayNames = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Resources = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scopes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    IsRegistrationNotCompleted = table.Column<bool>(type: "bit", nullable: false),
                    InvitationCreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganizationQuotaOverride = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_Groups_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PreviewFeatureAcceptances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviewFeature = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreviewFeatureAcceptances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreviewFeatureAcceptances_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SelfHostedOrganizationLicenses",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LicenseToken = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: true),
                    SelfHostedLicenseKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SelfHostedSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfHostedOrganizationLicenses", x => x.OrganizationId);
                    table.ForeignKey(
                        name: "FK_SelfHostedOrganizationLicenses_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServicePrincipals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConcurrencyToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApplicationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClientSecret = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConsentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayNames = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JsonWebKeySet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Permissions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostLogoutRedirectUris = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RedirectUris = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Requirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Settings = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePrincipals", x => x.Id);
                    table.UniqueConstraint("AK_ServicePrincipals_Id_OrganizationId", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ServicePrincipals_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TriggerBehaviourOnModified = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stacks", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_Stacks_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationUsers",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeactivated = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    InvitationToken = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    InvitationSentDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvitationExpirationDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvitationCompleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    InvitationCompletedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationUsers", x => new { x.UserId, x.OrganizationId });
                    table.UniqueConstraint("AK_OrganizationUsers_Id", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationUsers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSystemRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, computedColumnSql: "[UserId]", stored: false),
                    RoleName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSystemRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSystemRoleAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Authorizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConcurrencyToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Scopes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authorizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Authorizations_ServicePrincipals_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "ServicePrincipals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Runners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicePrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    AllowMultipleInstances = table.Column<bool>(type: "bit", nullable: false),
                    IsAssignedToAllModules = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Runners", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_Runners_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Runners_ServicePrincipals_ServicePrincipalId_OrganizationId",
                        columns: x => new { x.ServicePrincipalId, x.OrganizationId },
                        principalTable: "ServicePrincipals",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServicePrincipalSystemRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicePrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, computedColumnSql: "[ServicePrincipalId]", stored: false),
                    RoleName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePrincipalSystemRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicePrincipalSystemRoleAssignments_ServicePrincipals_ServicePrincipalId",
                        column: x => x.ServicePrincipalId,
                        principalTable: "ServicePrincipals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Namespaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DefaultInitBeforeHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    DefaultInitAfterHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    DefaultAutoUpgradeEnabled = table.Column<bool>(type: "bit", nullable: true),
                    DefaultAutoReconfigureEnabled = table.Column<bool>(type: "bit", nullable: true),
                    DefaultAutoMigrateEnabled = table.Column<bool>(type: "bit", nullable: true),
                    DefaultCleanInitEnabled = table.Column<bool>(type: "bit", nullable: true),
                    DefaultPlanBeforeHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    DefaultPlanAfterHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    DefaultPlanDestroyBeforeHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    DefaultPlanDestroyAfterHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    DefaultApplyBeforeHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    DefaultApplyAfterHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    DefaultOutputBeforeHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    DefaultOutputAfterHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    DefaultDestroyBeforeHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    DefaultDestroyAfterHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    DefaultValidateBeforeHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    DefaultValidateAfterHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    DefaultApplyApprovalThreshold = table.Column<int>(type: "int", nullable: true),
                    DefaultDestroyApprovalThreshold = table.Column<int>(type: "int", nullable: true),
                    DefaultApprovalTimeoutMinutes = table.Column<int>(type: "int", nullable: true),
                    TriggerBehaviourOnModified = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultEngine = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DefaultDriftCheckEnabled = table.Column<bool>(type: "bit", nullable: true),
                    DefaultDriftCheckIntervalMinutes = table.Column<int>(type: "int", nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Namespaces", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_Namespaces_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Namespaces_Stacks_StackId_OrganizationId",
                        columns: x => new { x.StackId, x.OrganizationId },
                        principalTable: "Stacks",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, computedColumnSql: "CASE WHEN [GroupMemberDiscriminator] = 'User' THEN [UserId] WHEN [GroupMemberDiscriminator] = 'ServicePrincipal' THEN [ServicePrincipalId] WHEN [GroupMemberDiscriminator] = 'Group' THEN [MemberGroupId] END", stored: true),
                    GroupMemberDiscriminator = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MemberGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServicePrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembers", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_GroupMembers_Groups_GroupId_OrganizationId",
                        columns: x => new { x.GroupId, x.OrganizationId },
                        principalTable: "Groups",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Groups_MemberGroupId_OrganizationId",
                        columns: x => new { x.MemberGroupId, x.OrganizationId },
                        principalTable: "Groups",
                        principalColumns: new[] { "Id", "OrganizationId" });
                    table.ForeignKey(
                        name: "FK_GroupMembers_OrganizationUsers_UserId_OrganizationId",
                        columns: x => new { x.UserId, x.OrganizationId },
                        principalTable: "OrganizationUsers",
                        principalColumns: new[] { "UserId", "OrganizationId" });
                    table.ForeignKey(
                        name: "FK_GroupMembers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupMembers_ServicePrincipals_ServicePrincipalId_OrganizationId",
                        columns: x => new { x.ServicePrincipalId, x.OrganizationId },
                        principalTable: "ServicePrincipals",
                        principalColumns: new[] { "Id", "OrganizationId" });
                });

            migrationBuilder.CreateTable(
                name: "OrganizationRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, computedColumnSql: "CASE WHEN [PrincipalDiscriminator] = 'User' THEN [UserId] WHEN [PrincipalDiscriminator] = 'ServicePrincipal' THEN [ServicePrincipalId] WHEN [PrincipalDiscriminator] = 'Group' THEN [GroupId] END", stored: true),
                    PrincipalDiscriminator = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServicePrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationRoleAssignments", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_OrganizationRoleAssignments_Groups_GroupId_OrganizationId",
                        columns: x => new { x.GroupId, x.OrganizationId },
                        principalTable: "Groups",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationRoleAssignments_OrganizationUsers_UserId_OrganizationId",
                        columns: x => new { x.UserId, x.OrganizationId },
                        principalTable: "OrganizationUsers",
                        principalColumns: new[] { "UserId", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationRoleAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationRoleAssignments_ServicePrincipals_ServicePrincipalId_OrganizationId",
                        columns: x => new { x.ServicePrincipalId, x.OrganizationId },
                        principalTable: "ServicePrincipals",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StackRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, computedColumnSql: "CASE WHEN [PrincipalDiscriminator] = 'User' THEN [UserId] WHEN [PrincipalDiscriminator] = 'ServicePrincipal' THEN [ServicePrincipalId] WHEN [PrincipalDiscriminator] = 'Group' THEN [GroupId] END", stored: true),
                    PrincipalDiscriminator = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServicePrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StackRoleAssignments", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_StackRoleAssignments_Groups_GroupId_OrganizationId",
                        columns: x => new { x.GroupId, x.OrganizationId },
                        principalTable: "Groups",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StackRoleAssignments_OrganizationUsers_UserId_OrganizationId",
                        columns: x => new { x.UserId, x.OrganizationId },
                        principalTable: "OrganizationUsers",
                        principalColumns: new[] { "UserId", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StackRoleAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StackRoleAssignments_ServicePrincipals_ServicePrincipalId_OrganizationId",
                        columns: x => new { x.ServicePrincipalId, x.OrganizationId },
                        principalTable: "ServicePrincipals",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StackRoleAssignments_Stacks_StackId_OrganizationId",
                        columns: x => new { x.StackId, x.OrganizationId },
                        principalTable: "Stacks",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuthorizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConcurrencyToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RedemptionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReferenceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tokens_Authorizations_AuthorizationId",
                        column: x => x.AuthorizationId,
                        principalTable: "Authorizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tokens_ServicePrincipals_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "ServicePrincipals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RunnerConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstanceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SignalRConnectionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ServerInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunnerConnections", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_RunnerConnections_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RunnerConnections_Runners_RunnerId_OrganizationId",
                        columns: x => new { x.RunnerId, x.OrganizationId },
                        principalTable: "Runners",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RunnerRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, computedColumnSql: "CASE WHEN [PrincipalDiscriminator] = 'User' THEN [UserId] WHEN [PrincipalDiscriminator] = 'ServicePrincipal' THEN [ServicePrincipalId] WHEN [PrincipalDiscriminator] = 'Group' THEN [GroupId] END", stored: true),
                    PrincipalDiscriminator = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServicePrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunnerRoleAssignments", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_RunnerRoleAssignments_Groups_GroupId_OrganizationId",
                        columns: x => new { x.GroupId, x.OrganizationId },
                        principalTable: "Groups",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RunnerRoleAssignments_OrganizationUsers_UserId_OrganizationId",
                        columns: x => new { x.UserId, x.OrganizationId },
                        principalTable: "OrganizationUsers",
                        principalColumns: new[] { "UserId", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RunnerRoleAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RunnerRoleAssignments_Runners_RunnerId_OrganizationId",
                        columns: x => new { x.RunnerId, x.OrganizationId },
                        principalTable: "Runners",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RunnerRoleAssignments_ServicePrincipals_ServicePrincipalId_OrganizationId",
                        columns: x => new { x.ServicePrincipalId, x.OrganizationId },
                        principalTable: "ServicePrincipals",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RunnerStackAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunnerStackAssignments", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_RunnerStackAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RunnerStackAssignments_Runners_RunnerId_OrganizationId",
                        columns: x => new { x.RunnerId, x.OrganizationId },
                        principalTable: "Runners",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RunnerStackAssignments_Stacks_StackId_OrganizationId",
                        columns: x => new { x.StackId, x.OrganizationId },
                        principalTable: "Stacks",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SourceRefresherPreselections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerInstanceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceRefresherPreselections", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_SourceRefresherPreselections_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceRefresherPreselections_Runners_RunnerId_OrganizationId",
                        columns: x => new { x.RunnerId, x.OrganizationId },
                        principalTable: "Runners",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerInstanceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    SourceRevision = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SourceSubdirectory = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ApplyApprovalThreshold = table.Column<int>(type: "int", nullable: true),
                    DestroyApprovalThreshold = table.Column<int>(type: "int", nullable: true),
                    ApprovalTimeoutMinutes = table.Column<int>(type: "int", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceRevisionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InitBeforeHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    InitAfterHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    IgnoreNamespaceBackendConfigs = table.Column<bool>(type: "bit", nullable: false),
                    IgnoreNamespaceExtraFiles = table.Column<bool>(type: "bit", nullable: false),
                    IgnoreNamespaceFlags = table.Column<bool>(type: "bit", nullable: false),
                    AutoUpgradeEnabled = table.Column<bool>(type: "bit", nullable: true),
                    AutoReconfigureEnabled = table.Column<bool>(type: "bit", nullable: true),
                    AutoMigrateEnabled = table.Column<bool>(type: "bit", nullable: true),
                    CleanInitEnabled = table.Column<bool>(type: "bit", nullable: true),
                    PlanBeforeHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    PlanAfterHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    PlanDestroyBeforeHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    PlanDestroyAfterHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    ApplyBeforeHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    ApplyAfterHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    OutputBeforeHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    OutputAfterHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    DestroyBeforeHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    DestroyAfterHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    ValidateBeforeHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    ValidateAfterHook = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    Engine = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WaitForApplyDependencies = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WaitForDestroyDependencies = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TriggerOnDefinitionChanged = table.Column<bool>(type: "bit", nullable: false),
                    TriggerOnUpstreamOutputChanged = table.Column<bool>(type: "bit", nullable: false),
                    TriggerOnSourceChanged = table.Column<bool>(type: "bit", nullable: false),
                    TriggerOnSourceChangedNotification = table.Column<bool>(type: "bit", nullable: false),
                    DriftCheckEnabled = table.Column<bool>(type: "bit", nullable: true),
                    DriftCheckIntervalMinutes = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_Modules_ModuleModifiedSaga_Id_OrganizationId",
                        columns: x => new { x.Id, x.OrganizationId },
                        principalTable: "ModuleModifiedSaga",
                        principalColumns: new[] { "CorrelationId", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Modules_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Modules_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Modules_Runners_RunnerId_OrganizationId",
                        columns: x => new { x.RunnerId, x.OrganizationId },
                        principalTable: "Runners",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NamespaceBackendConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_NamespaceBackendConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NamespaceBackendConfigs_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceBackendConfigs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NamespaceExtraFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Contents = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Overwrite = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NamespaceExtraFiles", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_NamespaceExtraFiles_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceExtraFiles_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NamespacePulumiArrayFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Task = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Flag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_NamespacePulumiArrayFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NamespacePulumiArrayFlags_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespacePulumiArrayFlags_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NamespacePulumiFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Task = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Flag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_NamespacePulumiFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NamespacePulumiFlags_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespacePulumiFlags_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NamespaceRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, computedColumnSql: "CASE WHEN [PrincipalDiscriminator] = 'User' THEN [UserId] WHEN [PrincipalDiscriminator] = 'ServicePrincipal' THEN [ServicePrincipalId] WHEN [PrincipalDiscriminator] = 'Group' THEN [GroupId] END", stored: true),
                    PrincipalDiscriminator = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServicePrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NamespaceRoleAssignments", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_NamespaceRoleAssignments_Groups_GroupId_OrganizationId",
                        columns: x => new { x.GroupId, x.OrganizationId },
                        principalTable: "Groups",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceRoleAssignments_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceRoleAssignments_OrganizationUsers_UserId_OrganizationId",
                        columns: x => new { x.UserId, x.OrganizationId },
                        principalTable: "OrganizationUsers",
                        principalColumns: new[] { "UserId", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceRoleAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NamespaceRoleAssignments_ServicePrincipals_ServicePrincipalId_OrganizationId",
                        columns: x => new { x.ServicePrincipalId, x.OrganizationId },
                        principalTable: "ServicePrincipals",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NamespaceTerraformArrayFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Task = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Flag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_NamespaceTerraformArrayFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NamespaceTerraformArrayFlags_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceTerraformArrayFlags_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NamespaceTerraformFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Task = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Flag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_NamespaceTerraformFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NamespaceTerraformFlags_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceTerraformFlags_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RunnerNamespaceAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_RunnerNamespaceAssignments", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_RunnerNamespaceAssignments_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RunnerNamespaceAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RunnerNamespaceAssignments_Runners_RunnerId_OrganizationId",
                        columns: x => new { x.RunnerId, x.OrganizationId },
                        principalTable: "Runners",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplyJobSagas",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentState = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ResponseAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GracefulCancellationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    KillCancellationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HeartbeatRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HeartbeatScheduleTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalTimeoutScheduleTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalTimeoutMinutes = table.Column<int>(type: "int", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RunnerInstanceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DeclaredJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsDeclined = table.Column<bool>(type: "bit", nullable: false),
                    PreviousStateBeforeWaiting = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PreviousStateBeforeCancelling = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    WaitingSince = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServerInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefinitiveRevision = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplyJobSagas", x => new { x.CorrelationId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ApplyJobSagas_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DependsOnModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DependsOnModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DependsOnModules", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_DependsOnModules_Modules_DependsOnModuleId_OrganizationId",
                        columns: x => new { x.DependsOnModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DependsOnModules_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DependsOnModules_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DestroyJobSagas",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentState = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ResponseAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GracefulCancellationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    KillCancellationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HeartbeatRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HeartbeatScheduleTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalTimeoutScheduleTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalTimeoutMinutes = table.Column<int>(type: "int", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RunnerInstanceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DeclaredJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsDeclined = table.Column<bool>(type: "bit", nullable: false),
                    PreviousStateBeforeWaiting = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PreviousStateBeforeCancelling = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    WaitingSince = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServerInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefinitiveRevision = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestroyJobSagas", x => new { x.CorrelationId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_DestroyJobSagas_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModuleBackendConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_ModuleBackendConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleBackendConfigs_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleBackendConfigs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModuleExtraFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Contents = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Overwrite = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleExtraFiles", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModuleExtraFiles_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleExtraFiles_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModuleJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimestampStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TimestampEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JobType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FailedOnServerSideStep = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServerSideErrorHeader = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ServerSideError = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true),
                    Logs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WaitingForApproval = table.Column<bool>(type: "bit", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: true),
                    DefinitiveRevision = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ActualStateHeadline = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputsUnchangedList = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OutputsCreateList = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OutputsModifyList = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OutputsDestroyList = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OutputsRecreateList = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleJobs", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModuleJobs_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleJobs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModulePulumiArrayFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Task = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Flag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_ModulePulumiArrayFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModulePulumiArrayFlags_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModulePulumiArrayFlags_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModulePulumiFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Task = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Flag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_ModulePulumiFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModulePulumiFlags_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModulePulumiFlags_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModuleRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, computedColumnSql: "CASE WHEN [PrincipalDiscriminator] = 'User' THEN [UserId] WHEN [PrincipalDiscriminator] = 'ServicePrincipal' THEN [ServicePrincipalId] WHEN [PrincipalDiscriminator] = 'Group' THEN [GroupId] END", stored: true),
                    PrincipalDiscriminator = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServicePrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleRoleAssignments", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModuleRoleAssignments_Groups_GroupId_OrganizationId",
                        columns: x => new { x.GroupId, x.OrganizationId },
                        principalTable: "Groups",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleRoleAssignments_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleRoleAssignments_OrganizationUsers_UserId_OrganizationId",
                        columns: x => new { x.UserId, x.OrganizationId },
                        principalTable: "OrganizationUsers",
                        principalColumns: new[] { "UserId", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleRoleAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModuleRoleAssignments_ServicePrincipals_ServicePrincipalId_OrganizationId",
                        columns: x => new { x.ServicePrincipalId, x.OrganizationId },
                        principalTable: "ServicePrincipals",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModuleSagas",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CurrentState = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DesiredStateHeadline = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QueuedDesiredStateHeadline = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QueuedReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DesiredDefinitiveRevision = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ActualResourceCount = table.Column<int>(type: "int", nullable: true),
                    DriftCheckScheduleTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleSagas", x => x.CorrelationId);
                    table.ForeignKey(
                        name: "FK_ModuleSagas_Modules_CorrelationId_OrganizationId",
                        columns: x => new { x.CorrelationId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModuleTerraformArrayFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Task = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Flag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_ModuleTerraformArrayFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleTerraformArrayFlags_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleTerraformArrayFlags_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModuleTerraformFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Task = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Flag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_ModuleTerraformFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleTerraformFlags_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleTerraformFlags_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutputSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<long>(type: "bigint", nullable: false),
                    Checksum = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutputSets", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_OutputSets_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OutputSets_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RunnerModuleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_RunnerModuleAssignments", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_RunnerModuleAssignments_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RunnerModuleAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RunnerModuleAssignments_Runners_RunnerId_OrganizationId",
                        columns: x => new { x.RunnerId, x.OrganizationId },
                        principalTable: "Runners",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Secrets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ScopeKind = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Discriminator = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StackId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Secrets", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_Secrets_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Secrets_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Secrets_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Secrets_Stacks_StackId_OrganizationId",
                        columns: x => new { x.StackId, x.OrganizationId },
                        principalTable: "Stacks",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VariableSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<long>(type: "bigint", nullable: false),
                    Checksum = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariableSets", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_VariableSets_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VariableSets_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobRunnerAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerIdentityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentTaskId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TasksCompleted = table.Column<int>(type: "int", nullable: false),
                    TasksTotal = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRunnerAssignments", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_JobRunnerAssignments_ModuleJobs_JobId_OrganizationId",
                        columns: x => new { x.JobId, x.OrganizationId },
                        principalTable: "ModuleJobs",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobRunnerAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModuleJobApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipalDiscriminator = table.Column<int>(type: "int", nullable: false),
                    DecisionDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Declined = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleJobApprovals", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModuleJobApprovals_ModuleJobs_ModuleJobId_OrganizationId",
                        columns: x => new { x.ModuleJobId, x.OrganizationId },
                        principalTable: "ModuleJobs",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleJobApprovals_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RunnerConnectionJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunnerConnectionJobs", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_RunnerConnectionJobs_ModuleJobs_ModuleJobId_OrganizationId",
                        columns: x => new { x.ModuleJobId, x.OrganizationId },
                        principalTable: "ModuleJobs",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RunnerConnectionJobs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RunnerConnectionJobs_RunnerConnections_RunnerConnectionId_OrganizationId",
                        columns: x => new { x.RunnerConnectionId, x.OrganizationId },
                        principalTable: "RunnerConnections",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Outputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutputSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FromExtraFile = table.Column<bool>(type: "bit", nullable: false),
                    Discriminator = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", maxLength: 32000, nullable: true),
                    RemoteSecretName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outputs", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_Outputs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Outputs_OutputSets_OutputSetId_OrganizationId",
                        columns: x => new { x.OutputSetId, x.OrganizationId },
                        principalTable: "OutputSets",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NamespaceInputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    InputKind = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UsageMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Discriminator = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: false),
                    DefinitionName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LiteralValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SecretId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NamespaceInputs", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_NamespaceInputs_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceInputs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NamespaceInputs_Secrets_SecretId_OrganizationId",
                        columns: x => new { x.SecretId, x.OrganizationId },
                        principalTable: "Secrets",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Variables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariableSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Sensitive = table.Column<bool>(type: "bit", nullable: false),
                    Nullable = table.Column<bool>(type: "bit", nullable: false),
                    FromExtraFile = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Variables", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_Variables_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Variables_VariableSets_VariableSetId_OrganizationId",
                        columns: x => new { x.VariableSetId, x.OrganizationId },
                        principalTable: "VariableSets",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModuleInputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    InputKind = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Discriminator = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: false),
                    DefinitionName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LiteralValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OutputModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OutputName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SecretId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NamespaceInputId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleInputs", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModuleInputs_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleInputs_Modules_OutputModuleId_OrganizationId",
                        columns: x => new { x.OutputModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModuleInputs_NamespaceInputs_NamespaceInputId_OrganizationId",
                        columns: x => new { x.NamespaceInputId, x.OrganizationId },
                        principalTable: "NamespaceInputs",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModuleInputs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModuleInputs_Secrets_SecretId_OrganizationId",
                        columns: x => new { x.SecretId, x.OrganizationId },
                        principalTable: "Secrets",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplyJobSagas_CorrelationId",
                table: "ApplyJobSagas",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplyJobSagas_ModuleId_OrganizationId",
                table: "ApplyJobSagas",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Authorizations_ApplicationId_Status_Subject_Type",
                table: "Authorizations",
                columns: new[] { "ApplicationId", "Status", "Subject", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_DependsOnModules_DependsOnModuleId_OrganizationId",
                table: "DependsOnModules",
                columns: new[] { "DependsOnModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_DependsOnModules_Id",
                table: "DependsOnModules",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DependsOnModules_ModuleId_DependsOnModuleId_OrganizationId",
                table: "DependsOnModules",
                columns: new[] { "ModuleId", "DependsOnModuleId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DependsOnModules_ModuleId_OrganizationId",
                table: "DependsOnModules",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_DependsOnModules_OrganizationId",
                table: "DependsOnModules",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_DestroyJobSagas_CorrelationId",
                table: "DestroyJobSagas",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DestroyJobSagas_ModuleId_OrganizationId",
                table: "DestroyJobSagas",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_GroupId_MemberGroupId_OrganizationId",
                table: "GroupMembers",
                columns: new[] { "GroupId", "MemberGroupId", "OrganizationId" },
                unique: true,
                filter: "[MemberGroupId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_GroupId_OrganizationId",
                table: "GroupMembers",
                columns: new[] { "GroupId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_GroupId_ServicePrincipalId_OrganizationId",
                table: "GroupMembers",
                columns: new[] { "GroupId", "ServicePrincipalId", "OrganizationId" },
                unique: true,
                filter: "[ServicePrincipalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_GroupId_UserId_OrganizationId",
                table: "GroupMembers",
                columns: new[] { "GroupId", "UserId", "OrganizationId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_Id",
                table: "GroupMembers",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_MemberGroupId",
                table: "GroupMembers",
                column: "MemberGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_MemberGroupId_GroupId_OrganizationId",
                table: "GroupMembers",
                columns: new[] { "MemberGroupId", "GroupId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_MemberGroupId_OrganizationId",
                table: "GroupMembers",
                columns: new[] { "MemberGroupId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_OrganizationId",
                table: "GroupMembers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_PrincipalId",
                table: "GroupMembers",
                column: "PrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_ServicePrincipalId",
                table: "GroupMembers",
                column: "ServicePrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_ServicePrincipalId_GroupId_OrganizationId",
                table: "GroupMembers",
                columns: new[] { "ServicePrincipalId", "GroupId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_ServicePrincipalId_OrganizationId",
                table: "GroupMembers",
                columns: new[] { "ServicePrincipalId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_UserId",
                table: "GroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_UserId_GroupId_OrganizationId",
                table: "GroupMembers",
                columns: new[] { "UserId", "GroupId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_UserId_OrganizationId",
                table: "GroupMembers",
                columns: new[] { "UserId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Id",
                table: "Groups",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Name_OrganizationId",
                table: "Groups",
                columns: new[] { "Name", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_OrganizationId",
                table: "Groups",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRunnerAssignment_JobId",
                table: "JobRunnerAssignments",
                column: "JobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobRunnerAssignment_RunnerIdentityId",
                table: "JobRunnerAssignments",
                column: "RunnerIdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRunnerAssignment_RunnerIdentityId_Status",
                table: "JobRunnerAssignments",
                columns: new[] { "RunnerIdentityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_JobRunnerAssignment_Status",
                table: "JobRunnerAssignments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JobRunnerAssignments_Id",
                table: "JobRunnerAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobRunnerAssignments_JobId_OrganizationId",
                table: "JobRunnerAssignments",
                columns: new[] { "JobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_JobRunnerAssignments_OrganizationId",
                table: "JobRunnerAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleBackendConfigs_ModuleId_Name",
                table: "ModuleBackendConfigs",
                columns: new[] { "ModuleId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleBackendConfigs_ModuleId_OrganizationId",
                table: "ModuleBackendConfigs",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleBackendConfigs_OrganizationId",
                table: "ModuleBackendConfigs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleExtraFiles_Id",
                table: "ModuleExtraFiles",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleExtraFiles_ModuleId_FileName",
                table: "ModuleExtraFiles",
                columns: new[] { "ModuleId", "FileName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleExtraFiles_ModuleId_OrganizationId",
                table: "ModuleExtraFiles",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleExtraFiles_OrganizationId",
                table: "ModuleExtraFiles",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleInputs_Id",
                table: "ModuleInputs",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleInputs_ModuleId",
                table: "ModuleInputs",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleInputs_ModuleId_InputKind_Name_OrganizationId",
                table: "ModuleInputs",
                columns: new[] { "ModuleId", "InputKind", "Name", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleInputs_ModuleId_OrganizationId",
                table: "ModuleInputs",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleInputs_NamespaceInputId",
                table: "ModuleInputs",
                column: "NamespaceInputId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleInputs_NamespaceInputId_OrganizationId",
                table: "ModuleInputs",
                columns: new[] { "NamespaceInputId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleInputs_OrganizationId",
                table: "ModuleInputs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleInputs_OutputModuleId",
                table: "ModuleInputs",
                column: "OutputModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleInputs_OutputModuleId_OrganizationId",
                table: "ModuleInputs",
                columns: new[] { "OutputModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleInputs_OutputModuleId_OutputName",
                table: "ModuleInputs",
                columns: new[] { "OutputModuleId", "OutputName" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleInputs_SecretId",
                table: "ModuleInputs",
                column: "SecretId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleInputs_SecretId_OrganizationId",
                table: "ModuleInputs",
                columns: new[] { "SecretId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobApprovals_Id",
                table: "ModuleJobApprovals",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobApprovals_ModuleJobId",
                table: "ModuleJobApprovals",
                column: "ModuleJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobApprovals_ModuleJobId_OrganizationId",
                table: "ModuleJobApprovals",
                columns: new[] { "ModuleJobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobApprovals_ModuleJobId_PrincipalId_OrganizationId",
                table: "ModuleJobApprovals",
                columns: new[] { "ModuleJobId", "PrincipalId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobApprovals_OrganizationId",
                table: "ModuleJobApprovals",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobApprovals_PrincipalId",
                table: "ModuleJobApprovals",
                column: "PrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobs_Id",
                table: "ModuleJobs",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobs_ModuleId",
                table: "ModuleJobs",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobs_ModuleId_IsCurrent_OrganizationId",
                table: "ModuleJobs",
                columns: new[] { "ModuleId", "IsCurrent", "OrganizationId" },
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobs_ModuleId_OrganizationId",
                table: "ModuleJobs",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobs_ModuleId_TimestampEnd_OrganizationId",
                table: "ModuleJobs",
                columns: new[] { "ModuleId", "TimestampEnd", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobs_ModuleId_TimestampStart_OrganizationId",
                table: "ModuleJobs",
                columns: new[] { "ModuleId", "TimestampStart", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobs_OrganizationId",
                table: "ModuleJobs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleModifiedSaga_CorrelationId",
                table: "ModuleModifiedSaga",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModulePulumiArrayFlags_ModuleId_OrganizationId",
                table: "ModulePulumiArrayFlags",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModulePulumiArrayFlags_ModuleId_Task_Flag_Value",
                table: "ModulePulumiArrayFlags",
                columns: new[] { "ModuleId", "Task", "Flag", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModulePulumiArrayFlags_OrganizationId",
                table: "ModulePulumiArrayFlags",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModulePulumiFlags_ModuleId_OrganizationId",
                table: "ModulePulumiFlags",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModulePulumiFlags_ModuleId_Task_Flag",
                table: "ModulePulumiFlags",
                columns: new[] { "ModuleId", "Task", "Flag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModulePulumiFlags_OrganizationId",
                table: "ModulePulumiFlags",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoleAssignments_GroupId",
                table: "ModuleRoleAssignments",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoleAssignments_GroupId_ModuleId_OrganizationId_RoleName",
                table: "ModuleRoleAssignments",
                columns: new[] { "GroupId", "ModuleId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[GroupId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoleAssignments_GroupId_OrganizationId",
                table: "ModuleRoleAssignments",
                columns: new[] { "GroupId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoleAssignments_Id",
                table: "ModuleRoleAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoleAssignments_ModuleId",
                table: "ModuleRoleAssignments",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoleAssignments_ModuleId_OrganizationId",
                table: "ModuleRoleAssignments",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoleAssignments_OrganizationId",
                table: "ModuleRoleAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoleAssignments_PrincipalId",
                table: "ModuleRoleAssignments",
                column: "PrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoleAssignments_ServicePrincipalId",
                table: "ModuleRoleAssignments",
                column: "ServicePrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoleAssignments_ServicePrincipalId_ModuleId_OrganizationId_RoleName",
                table: "ModuleRoleAssignments",
                columns: new[] { "ServicePrincipalId", "ModuleId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[ServicePrincipalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoleAssignments_ServicePrincipalId_OrganizationId",
                table: "ModuleRoleAssignments",
                columns: new[] { "ServicePrincipalId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoleAssignments_UserId",
                table: "ModuleRoleAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoleAssignments_UserId_ModuleId_OrganizationId_RoleName",
                table: "ModuleRoleAssignments",
                columns: new[] { "UserId", "ModuleId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoleAssignments_UserId_OrganizationId",
                table: "ModuleRoleAssignments",
                columns: new[] { "UserId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Module_CreatedDateTime",
                table: "Modules",
                column: "CreatedDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_Id",
                table: "Modules",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Modules_NamespaceId",
                table: "Modules",
                column: "NamespaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_NamespaceId_Id",
                table: "Modules",
                columns: new[] { "NamespaceId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Modules_NamespaceId_OrganizationId",
                table: "Modules",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Modules_OrganizationId_NamespaceId_Name",
                table: "Modules",
                columns: new[] { "OrganizationId", "NamespaceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Modules_RunnerId",
                table: "Modules",
                column: "RunnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_RunnerId_OrganizationId",
                table: "Modules",
                columns: new[] { "RunnerId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Modules_TriggerOnUpstreamOutputChanged",
                table: "Modules",
                column: "TriggerOnUpstreamOutputChanged");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleSagas_CorrelationId",
                table: "ModuleSagas",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleSagas_CorrelationId_OrganizationId",
                table: "ModuleSagas",
                columns: new[] { "CorrelationId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTerraformArrayFlags_ModuleId_OrganizationId",
                table: "ModuleTerraformArrayFlags",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTerraformArrayFlags_ModuleId_Task_Flag_Value",
                table: "ModuleTerraformArrayFlags",
                columns: new[] { "ModuleId", "Task", "Flag", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTerraformArrayFlags_OrganizationId",
                table: "ModuleTerraformArrayFlags",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTerraformFlags_ModuleId_OrganizationId",
                table: "ModuleTerraformFlags",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTerraformFlags_ModuleId_Task_Flag",
                table: "ModuleTerraformFlags",
                columns: new[] { "ModuleId", "Task", "Flag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTerraformFlags_OrganizationId",
                table: "ModuleTerraformFlags",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceBackendConfigs_NamespaceId_Name",
                table: "NamespaceBackendConfigs",
                columns: new[] { "NamespaceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceBackendConfigs_NamespaceId_OrganizationId",
                table: "NamespaceBackendConfigs",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceBackendConfigs_OrganizationId",
                table: "NamespaceBackendConfigs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceExtraFiles_Id",
                table: "NamespaceExtraFiles",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceExtraFiles_NamespaceId_FileName",
                table: "NamespaceExtraFiles",
                columns: new[] { "NamespaceId", "FileName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceExtraFiles_NamespaceId_OrganizationId",
                table: "NamespaceExtraFiles",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceExtraFiles_OrganizationId",
                table: "NamespaceExtraFiles",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceInputs_Id",
                table: "NamespaceInputs",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceInputs_NamespaceId",
                table: "NamespaceInputs",
                column: "NamespaceId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceInputs_NamespaceId_InputKind_Name_OrganizationId",
                table: "NamespaceInputs",
                columns: new[] { "NamespaceId", "InputKind", "Name", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceInputs_NamespaceId_OrganizationId",
                table: "NamespaceInputs",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceInputs_OrganizationId",
                table: "NamespaceInputs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceInputs_SecretId_OrganizationId",
                table: "NamespaceInputs",
                columns: new[] { "SecretId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespacePulumiArrayFlags_NamespaceId_OrganizationId",
                table: "NamespacePulumiArrayFlags",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespacePulumiArrayFlags_NamespaceId_Task_Flag_Value",
                table: "NamespacePulumiArrayFlags",
                columns: new[] { "NamespaceId", "Task", "Flag", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespacePulumiArrayFlags_OrganizationId",
                table: "NamespacePulumiArrayFlags",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespacePulumiFlags_NamespaceId_OrganizationId",
                table: "NamespacePulumiFlags",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespacePulumiFlags_NamespaceId_Task_Flag",
                table: "NamespacePulumiFlags",
                columns: new[] { "NamespaceId", "Task", "Flag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespacePulumiFlags_OrganizationId",
                table: "NamespacePulumiFlags",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceRoleAssignments_GroupId",
                table: "NamespaceRoleAssignments",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceRoleAssignments_GroupId_NamespaceId_OrganizationId_RoleName",
                table: "NamespaceRoleAssignments",
                columns: new[] { "GroupId", "NamespaceId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[GroupId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceRoleAssignments_GroupId_OrganizationId",
                table: "NamespaceRoleAssignments",
                columns: new[] { "GroupId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceRoleAssignments_Id",
                table: "NamespaceRoleAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceRoleAssignments_NamespaceId",
                table: "NamespaceRoleAssignments",
                column: "NamespaceId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceRoleAssignments_NamespaceId_OrganizationId",
                table: "NamespaceRoleAssignments",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceRoleAssignments_OrganizationId",
                table: "NamespaceRoleAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceRoleAssignments_PrincipalId",
                table: "NamespaceRoleAssignments",
                column: "PrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceRoleAssignments_ServicePrincipalId",
                table: "NamespaceRoleAssignments",
                column: "ServicePrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceRoleAssignments_ServicePrincipalId_NamespaceId_OrganizationId_RoleName",
                table: "NamespaceRoleAssignments",
                columns: new[] { "ServicePrincipalId", "NamespaceId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[ServicePrincipalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceRoleAssignments_ServicePrincipalId_OrganizationId",
                table: "NamespaceRoleAssignments",
                columns: new[] { "ServicePrincipalId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceRoleAssignments_UserId",
                table: "NamespaceRoleAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceRoleAssignments_UserId_NamespaceId_OrganizationId_RoleName",
                table: "NamespaceRoleAssignments",
                columns: new[] { "UserId", "NamespaceId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceRoleAssignments_UserId_OrganizationId",
                table: "NamespaceRoleAssignments",
                columns: new[] { "UserId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Namespace_CreatedDateTime",
                table: "Namespaces",
                column: "CreatedDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Namespaces_Id",
                table: "Namespaces",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Namespaces_OrganizationId_StackId_Name",
                table: "Namespaces",
                columns: new[] { "OrganizationId", "StackId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Namespaces_StackId",
                table: "Namespaces",
                column: "StackId");

            migrationBuilder.CreateIndex(
                name: "IX_Namespaces_StackId_Id",
                table: "Namespaces",
                columns: new[] { "StackId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Namespaces_StackId_OrganizationId",
                table: "Namespaces",
                columns: new[] { "StackId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceTerraformArrayFlags_NamespaceId_OrganizationId",
                table: "NamespaceTerraformArrayFlags",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceTerraformArrayFlags_NamespaceId_Task_Flag_Value",
                table: "NamespaceTerraformArrayFlags",
                columns: new[] { "NamespaceId", "Task", "Flag", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceTerraformArrayFlags_OrganizationId",
                table: "NamespaceTerraformArrayFlags",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceTerraformFlags_NamespaceId_OrganizationId",
                table: "NamespaceTerraformFlags",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceTerraformFlags_NamespaceId_Task_Flag",
                table: "NamespaceTerraformFlags",
                columns: new[] { "NamespaceId", "Task", "Flag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceTerraformFlags_OrganizationId",
                table: "NamespaceTerraformFlags",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoleAssignments_GroupId",
                table: "OrganizationRoleAssignments",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoleAssignments_GroupId_OrganizationId_RoleName",
                table: "OrganizationRoleAssignments",
                columns: new[] { "GroupId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[GroupId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoleAssignments_Id",
                table: "OrganizationRoleAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoleAssignments_OrganizationId",
                table: "OrganizationRoleAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoleAssignments_PrincipalId",
                table: "OrganizationRoleAssignments",
                column: "PrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoleAssignments_ServicePrincipalId",
                table: "OrganizationRoleAssignments",
                column: "ServicePrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoleAssignments_ServicePrincipalId_OrganizationId_RoleName",
                table: "OrganizationRoleAssignments",
                columns: new[] { "ServicePrincipalId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[ServicePrincipalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoleAssignments_UserId",
                table: "OrganizationRoleAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoleAssignments_UserId_OrganizationId_RoleName",
                table: "OrganizationRoleAssignments",
                columns: new[] { "UserId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_CreatedDateTime",
                table: "Organizations",
                column: "CreatedDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Name",
                table: "Organizations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Status",
                table: "Organizations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_InvitationToken",
                table: "OrganizationUsers",
                column: "InvitationToken",
                unique: true,
                filter: "InvitationToken IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_JoinedAt",
                table: "OrganizationUsers",
                column: "JoinedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_OrganizationId",
                table: "OrganizationUsers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_OrganizationId_UserId",
                table: "OrganizationUsers",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true,
                filter: "UserId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_UserId",
                table: "OrganizationUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Outputs_Id",
                table: "Outputs",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Outputs_OrganizationId",
                table: "Outputs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Outputs_OutputSetId",
                table: "Outputs",
                column: "OutputSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Outputs_OutputSetId_Name_OrganizationId",
                table: "Outputs",
                columns: new[] { "OutputSetId", "Name", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Outputs_OutputSetId_OrganizationId",
                table: "Outputs",
                columns: new[] { "OutputSetId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutputSets_Checksum_ModuleId_Timestamp_OrganizationId",
                table: "OutputSets",
                columns: new[] { "Checksum", "ModuleId", "Timestamp", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutputSets_Id",
                table: "OutputSets",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutputSets_ModuleId",
                table: "OutputSets",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_OutputSets_ModuleId_OrganizationId",
                table: "OutputSets",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutputSets_ModuleId_Timestamp_Checksum_OrganizationId",
                table: "OutputSets",
                columns: new[] { "ModuleId", "Timestamp", "Checksum", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutputSets_ModuleId_Timestamp_OrganizationId",
                table: "OutputSets",
                columns: new[] { "ModuleId", "Timestamp", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutputSets_OrganizationId",
                table: "OutputSets",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PreviewFeatureAcceptance_OrgId_Feature",
                table: "PreviewFeatureAcceptances",
                columns: new[] { "OrganizationId", "PreviewFeature" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionJobs_Id",
                table: "RunnerConnectionJobs",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionJobs_ModuleJobId_OrganizationId",
                table: "RunnerConnectionJobs",
                columns: new[] { "ModuleJobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionJobs_OrganizationId",
                table: "RunnerConnectionJobs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionJobs_RunnerConnectionId_ModuleJobId_OrganizationId",
                table: "RunnerConnectionJobs",
                columns: new[] { "RunnerConnectionId", "ModuleJobId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionJobs_RunnerConnectionId_OrganizationId",
                table: "RunnerConnectionJobs",
                columns: new[] { "RunnerConnectionId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnections_Id",
                table: "RunnerConnections",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnections_OrganizationId_RunnerId_InstanceName",
                table: "RunnerConnections",
                columns: new[] { "OrganizationId", "RunnerId", "InstanceName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnections_OrganizationId_SignalRConnectionId",
                table: "RunnerConnections",
                columns: new[] { "OrganizationId", "SignalRConnectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnections_RunnerId_OrganizationId",
                table: "RunnerConnections",
                columns: new[] { "RunnerId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnections_ServerInstanceId",
                table: "RunnerConnections",
                column: "ServerInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerModuleAssignments_Id",
                table: "RunnerModuleAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunnerModuleAssignments_ModuleId",
                table: "RunnerModuleAssignments",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerModuleAssignments_ModuleId_OrganizationId",
                table: "RunnerModuleAssignments",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerModuleAssignments_ModuleId_RunnerId_OrganizationId",
                table: "RunnerModuleAssignments",
                columns: new[] { "ModuleId", "RunnerId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunnerModuleAssignments_OrganizationId",
                table: "RunnerModuleAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerModuleAssignments_RunnerId",
                table: "RunnerModuleAssignments",
                column: "RunnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerModuleAssignments_RunnerId_OrganizationId",
                table: "RunnerModuleAssignments",
                columns: new[] { "RunnerId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerNamespaceAssignments_Id",
                table: "RunnerNamespaceAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunnerNamespaceAssignments_NamespaceId",
                table: "RunnerNamespaceAssignments",
                column: "NamespaceId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerNamespaceAssignments_NamespaceId_OrganizationId",
                table: "RunnerNamespaceAssignments",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerNamespaceAssignments_NamespaceId_RunnerId_OrganizationId",
                table: "RunnerNamespaceAssignments",
                columns: new[] { "NamespaceId", "RunnerId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunnerNamespaceAssignments_OrganizationId",
                table: "RunnerNamespaceAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerNamespaceAssignments_RunnerId",
                table: "RunnerNamespaceAssignments",
                column: "RunnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerNamespaceAssignments_RunnerId_OrganizationId",
                table: "RunnerNamespaceAssignments",
                columns: new[] { "RunnerId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerRoleAssignments_GroupId",
                table: "RunnerRoleAssignments",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerRoleAssignments_GroupId_OrganizationId",
                table: "RunnerRoleAssignments",
                columns: new[] { "GroupId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerRoleAssignments_GroupId_RunnerId_OrganizationId_RoleName",
                table: "RunnerRoleAssignments",
                columns: new[] { "GroupId", "RunnerId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[GroupId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerRoleAssignments_Id",
                table: "RunnerRoleAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunnerRoleAssignments_OrganizationId",
                table: "RunnerRoleAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerRoleAssignments_PrincipalId",
                table: "RunnerRoleAssignments",
                column: "PrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerRoleAssignments_RunnerId",
                table: "RunnerRoleAssignments",
                column: "RunnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerRoleAssignments_RunnerId_OrganizationId",
                table: "RunnerRoleAssignments",
                columns: new[] { "RunnerId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerRoleAssignments_ServicePrincipalId",
                table: "RunnerRoleAssignments",
                column: "ServicePrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerRoleAssignments_ServicePrincipalId_OrganizationId",
                table: "RunnerRoleAssignments",
                columns: new[] { "ServicePrincipalId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerRoleAssignments_ServicePrincipalId_RunnerId_OrganizationId_RoleName",
                table: "RunnerRoleAssignments",
                columns: new[] { "ServicePrincipalId", "RunnerId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[ServicePrincipalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerRoleAssignments_UserId",
                table: "RunnerRoleAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerRoleAssignments_UserId_OrganizationId",
                table: "RunnerRoleAssignments",
                columns: new[] { "UserId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerRoleAssignments_UserId_RunnerId_OrganizationId_RoleName",
                table: "RunnerRoleAssignments",
                columns: new[] { "UserId", "RunnerId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Runner_CreatedDateTime",
                table: "Runners",
                column: "CreatedDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Runners_Id",
                table: "Runners",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Runners_Name_OrganizationId",
                table: "Runners",
                columns: new[] { "Name", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Runners_OrganizationId",
                table: "Runners",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Runners_ServicePrincipalId",
                table: "Runners",
                column: "ServicePrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_Runners_ServicePrincipalId_OrganizationId",
                table: "Runners",
                columns: new[] { "ServicePrincipalId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerStackAssignments_Id",
                table: "RunnerStackAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunnerStackAssignments_OrganizationId",
                table: "RunnerStackAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerStackAssignments_RunnerId",
                table: "RunnerStackAssignments",
                column: "RunnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerStackAssignments_RunnerId_OrganizationId",
                table: "RunnerStackAssignments",
                columns: new[] { "RunnerId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerStackAssignments_StackId",
                table: "RunnerStackAssignments",
                column: "StackId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerStackAssignments_StackId_OrganizationId",
                table: "RunnerStackAssignments",
                columns: new[] { "StackId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerStackAssignments_StackId_RunnerId_OrganizationId",
                table: "RunnerStackAssignments",
                columns: new[] { "StackId", "RunnerId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scopes_Name",
                table: "Scopes",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Secrets_Id",
                table: "Secrets",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Secrets_ModuleId",
                table: "Secrets",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Secrets_ModuleId_OrganizationId",
                table: "Secrets",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Secrets_Name_ModuleId",
                table: "Secrets",
                columns: new[] { "Name", "ModuleId" },
                unique: true,
                filter: "[ModuleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Secrets_Name_NamespaceId",
                table: "Secrets",
                columns: new[] { "Name", "NamespaceId" },
                unique: true,
                filter: "[NamespaceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Secrets_Name_StackId",
                table: "Secrets",
                columns: new[] { "Name", "StackId" },
                unique: true,
                filter: "[StackId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Secrets_NamespaceId",
                table: "Secrets",
                column: "NamespaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Secrets_NamespaceId_OrganizationId",
                table: "Secrets",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Secrets_OrganizationId",
                table: "Secrets",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Secrets_StackId",
                table: "Secrets",
                column: "StackId");

            migrationBuilder.CreateIndex(
                name: "IX_Secrets_StackId_OrganizationId",
                table: "Secrets",
                columns: new[] { "StackId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipals_ClientId",
                table: "ServicePrincipals",
                column: "ClientId",
                unique: true,
                filter: "[ClientId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipals_ClientId_OrganizationId",
                table: "ServicePrincipals",
                columns: new[] { "ClientId", "OrganizationId" },
                unique: true,
                filter: "[ClientId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipals_OrganizationId",
                table: "ServicePrincipals",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipalSystemRoleAssignments_Id",
                table: "ServicePrincipalSystemRoleAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipalSystemRoleAssignments_ServicePrincipalId",
                table: "ServicePrincipalSystemRoleAssignments",
                column: "ServicePrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipalSystemRoleAssignments_ServicePrincipalId_RoleName",
                table: "ServicePrincipalSystemRoleAssignments",
                columns: new[] { "ServicePrincipalId", "RoleName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourceRefresherPreselections_Id",
                table: "SourceRefresherPreselections",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourceRefresherPreselections_OrganizationId",
                table: "SourceRefresherPreselections",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRefresherPreselections_RunnerId_OrganizationId",
                table: "SourceRefresherPreselections",
                columns: new[] { "RunnerId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SourceRefresherPreselections_SourceUrl_OrganizationId",
                table: "SourceRefresherPreselections",
                columns: new[] { "SourceUrl", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StackRoleAssignments_GroupId",
                table: "StackRoleAssignments",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_StackRoleAssignments_GroupId_OrganizationId",
                table: "StackRoleAssignments",
                columns: new[] { "GroupId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StackRoleAssignments_GroupId_StackId_OrganizationId_RoleName",
                table: "StackRoleAssignments",
                columns: new[] { "GroupId", "StackId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[GroupId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StackRoleAssignments_Id",
                table: "StackRoleAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StackRoleAssignments_OrganizationId",
                table: "StackRoleAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_StackRoleAssignments_PrincipalId",
                table: "StackRoleAssignments",
                column: "PrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_StackRoleAssignments_ServicePrincipalId",
                table: "StackRoleAssignments",
                column: "ServicePrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_StackRoleAssignments_ServicePrincipalId_OrganizationId",
                table: "StackRoleAssignments",
                columns: new[] { "ServicePrincipalId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StackRoleAssignments_ServicePrincipalId_StackId_OrganizationId_RoleName",
                table: "StackRoleAssignments",
                columns: new[] { "ServicePrincipalId", "StackId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[ServicePrincipalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StackRoleAssignments_StackId",
                table: "StackRoleAssignments",
                column: "StackId");

            migrationBuilder.CreateIndex(
                name: "IX_StackRoleAssignments_StackId_OrganizationId",
                table: "StackRoleAssignments",
                columns: new[] { "StackId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StackRoleAssignments_UserId",
                table: "StackRoleAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StackRoleAssignments_UserId_OrganizationId",
                table: "StackRoleAssignments",
                columns: new[] { "UserId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StackRoleAssignments_UserId_StackId_OrganizationId_RoleName",
                table: "StackRoleAssignments",
                columns: new[] { "UserId", "StackId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Stack_CreatedDateTime",
                table: "Stacks",
                column: "CreatedDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Stacks_Id",
                table: "Stacks",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stacks_OrganizationId_Name",
                table: "Stacks",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_ApplicationId_Status_Subject_Type",
                table: "Tokens",
                columns: new[] { "ApplicationId", "Status", "Subject", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_AuthorizationId",
                table: "Tokens",
                column: "AuthorizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_ReferenceId",
                table: "Tokens",
                column: "ReferenceId",
                unique: true,
                filter: "[ReferenceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_User_CreatedDateTime",
                table: "Users",
                column: "CreatedDateTime");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserSystemRoleAssignments_Id",
                table: "UserSystemRoleAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSystemRoleAssignments_UserId",
                table: "UserSystemRoleAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSystemRoleAssignments_UserId_RoleName",
                table: "UserSystemRoleAssignments",
                columns: new[] { "UserId", "RoleName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Variables_Id",
                table: "Variables",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Variables_OrganizationId",
                table: "Variables",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Variables_VariableSetId",
                table: "Variables",
                column: "VariableSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Variables_VariableSetId_Name_OrganizationId",
                table: "Variables",
                columns: new[] { "VariableSetId", "Name", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Variables_VariableSetId_OrganizationId",
                table: "Variables",
                columns: new[] { "VariableSetId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_VariableSets_Checksum_ModuleId_Timestamp_OrganizationId",
                table: "VariableSets",
                columns: new[] { "Checksum", "ModuleId", "Timestamp", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_VariableSets_Id",
                table: "VariableSets",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VariableSets_ModuleId",
                table: "VariableSets",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_VariableSets_ModuleId_OrganizationId",
                table: "VariableSets",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_VariableSets_ModuleId_Timestamp_Checksum_OrganizationId",
                table: "VariableSets",
                columns: new[] { "ModuleId", "Timestamp", "Checksum", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VariableSets_ModuleId_Timestamp_OrganizationId",
                table: "VariableSets",
                columns: new[] { "ModuleId", "Timestamp", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_VariableSets_OrganizationId",
                table: "VariableSets",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplyJobSagas");

            migrationBuilder.DropTable(
                name: "DependsOnModules");

            migrationBuilder.DropTable(
                name: "DestroyJobSagas");

            migrationBuilder.DropTable(
                name: "GroupMembers");

            migrationBuilder.DropTable(
                name: "JobRunnerAssignments");

            migrationBuilder.DropTable(
                name: "ModuleBackendConfigs");

            migrationBuilder.DropTable(
                name: "ModuleExtraFiles");

            migrationBuilder.DropTable(
                name: "ModuleInputs");

            migrationBuilder.DropTable(
                name: "ModuleJobApprovals");

            migrationBuilder.DropTable(
                name: "ModulePulumiArrayFlags");

            migrationBuilder.DropTable(
                name: "ModulePulumiFlags");

            migrationBuilder.DropTable(
                name: "ModuleRoleAssignments");

            migrationBuilder.DropTable(
                name: "ModuleSagas");

            migrationBuilder.DropTable(
                name: "ModuleTerraformArrayFlags");

            migrationBuilder.DropTable(
                name: "ModuleTerraformFlags");

            migrationBuilder.DropTable(
                name: "NamespaceBackendConfigs");

            migrationBuilder.DropTable(
                name: "NamespaceExtraFiles");

            migrationBuilder.DropTable(
                name: "NamespacePulumiArrayFlags");

            migrationBuilder.DropTable(
                name: "NamespacePulumiFlags");

            migrationBuilder.DropTable(
                name: "NamespaceRoleAssignments");

            migrationBuilder.DropTable(
                name: "NamespaceTerraformArrayFlags");

            migrationBuilder.DropTable(
                name: "NamespaceTerraformFlags");

            migrationBuilder.DropTable(
                name: "OrganizationRoleAssignments");

            migrationBuilder.DropTable(
                name: "Outputs");

            migrationBuilder.DropTable(
                name: "PreviewFeatureAcceptances");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "RunnerConnectionJobs");

            migrationBuilder.DropTable(
                name: "RunnerModuleAssignments");

            migrationBuilder.DropTable(
                name: "RunnerNamespaceAssignments");

            migrationBuilder.DropTable(
                name: "RunnerRoleAssignments");

            migrationBuilder.DropTable(
                name: "RunnerStackAssignments");

            migrationBuilder.DropTable(
                name: "Scopes");

            migrationBuilder.DropTable(
                name: "SelfHostedOrganizationLicenses");

            migrationBuilder.DropTable(
                name: "ServicePrincipalSystemRoleAssignments");

            migrationBuilder.DropTable(
                name: "SourceRefresherPreselections");

            migrationBuilder.DropTable(
                name: "StackRoleAssignments");

            migrationBuilder.DropTable(
                name: "Tokens");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserSystemRoleAssignments");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "Variables");

            migrationBuilder.DropTable(
                name: "NamespaceInputs");

            migrationBuilder.DropTable(
                name: "OutputSets");

            migrationBuilder.DropTable(
                name: "ModuleJobs");

            migrationBuilder.DropTable(
                name: "RunnerConnections");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "OrganizationUsers");

            migrationBuilder.DropTable(
                name: "Authorizations");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "VariableSets");

            migrationBuilder.DropTable(
                name: "Secrets");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropTable(
                name: "ModuleModifiedSaga");

            migrationBuilder.DropTable(
                name: "Namespaces");

            migrationBuilder.DropTable(
                name: "Runners");

            migrationBuilder.DropTable(
                name: "Stacks");

            migrationBuilder.DropTable(
                name: "ServicePrincipals");

            migrationBuilder.DropTable(
                name: "Organizations");
        }
    }
}
