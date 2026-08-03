// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnapCd.Server.Host.Database.Migrations
{
    /// <inheritdoc />
    public partial class ConvertEnumColumnsToText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MissionType",
                table: "ModuleJobMissions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "MissionType",
                table: "ModuleJobMissionRuns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "PrincipalDiscriminator",
                table: "ModuleJobApprovals",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            // The type change turns stored ints into their numeric strings ('0', '1', ...);
            // map them to the enum member names the string conversion reads and writes.
            migrationBuilder.Sql("""
                UPDATE ModuleJobMissions SET MissionType = CASE MissionType
                    WHEN '0' THEN 'AutoDiagnose'
                    WHEN '1' THEN 'ApprovalRecommend'
                    WHEN '2' THEN 'SummarizeJob'
                    WHEN '3' THEN 'AutoFix'
                    ELSE MissionType END;

                UPDATE ModuleJobMissionRuns SET MissionType = CASE MissionType
                    WHEN '0' THEN 'AutoDiagnose'
                    WHEN '1' THEN 'ApprovalRecommend'
                    WHEN '2' THEN 'SummarizeJob'
                    WHEN '3' THEN 'AutoFix'
                    ELSE MissionType END;

                UPDATE ModuleJobApprovals SET PrincipalDiscriminator = CASE PrincipalDiscriminator
                    WHEN '0' THEN 'User'
                    WHEN '1' THEN 'ServicePrincipal'
                    ELSE PrincipalDiscriminator END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE ModuleJobMissions SET MissionType = CASE MissionType
                    WHEN 'AutoDiagnose' THEN '0'
                    WHEN 'ApprovalRecommend' THEN '1'
                    WHEN 'SummarizeJob' THEN '2'
                    WHEN 'AutoFix' THEN '3'
                    ELSE MissionType END;

                UPDATE ModuleJobMissionRuns SET MissionType = CASE MissionType
                    WHEN 'AutoDiagnose' THEN '0'
                    WHEN 'ApprovalRecommend' THEN '1'
                    WHEN 'SummarizeJob' THEN '2'
                    WHEN 'AutoFix' THEN '3'
                    ELSE MissionType END;

                UPDATE ModuleJobApprovals SET PrincipalDiscriminator = CASE PrincipalDiscriminator
                    WHEN 'User' THEN '0'
                    WHEN 'ServicePrincipal' THEN '1'
                    ELSE PrincipalDiscriminator END;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "MissionType",
                table: "ModuleJobMissions",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "MissionType",
                table: "ModuleJobMissionRuns",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "PrincipalDiscriminator",
                table: "ModuleJobApprovals",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
