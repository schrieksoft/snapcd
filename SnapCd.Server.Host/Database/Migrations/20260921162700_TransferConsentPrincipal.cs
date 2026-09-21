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
    public partial class TransferConsentPrincipal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReceiverConsentDecidedByPrincipalDiscriminator",
                table: "Transfers",
                newName: "SourceMergedDeclaredByPrincipalDiscriminator");

            migrationBuilder.RenameColumn(
                name: "ReceiverConsentDecidedBy",
                table: "Transfers",
                newName: "ReceiverConsentPrincipalId");

            migrationBuilder.AddColumn<Guid>(
                name: "ReceiverConsentAgentId",
                table: "Transfers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverConsentPrincipalDiscriminator",
                table: "Transfers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverLockedByPrincipalDiscriminator",
                table: "Transfers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverMergedDeclaredByPrincipalDiscriminator",
                table: "Transfers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceLockedByPrincipalDiscriminator",
                table: "Transfers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiverConsentAgentId",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ReceiverConsentPrincipalDiscriminator",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ReceiverLockedByPrincipalDiscriminator",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ReceiverMergedDeclaredByPrincipalDiscriminator",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "SourceLockedByPrincipalDiscriminator",
                table: "Transfers");

            migrationBuilder.RenameColumn(
                name: "SourceMergedDeclaredByPrincipalDiscriminator",
                table: "Transfers",
                newName: "ReceiverConsentDecidedByPrincipalDiscriminator");

            migrationBuilder.RenameColumn(
                name: "ReceiverConsentPrincipalId",
                table: "Transfers",
                newName: "ReceiverConsentDecidedBy");
        }
    }
}
