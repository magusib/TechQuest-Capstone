using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechQuestBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingNpcDeploymentQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NPC_deployment_MapDifficulty_PositionMarker",
                table: "NPC_deployment");

            migrationBuilder.Sql(
                """
                UPDATE "NPC_deployment"
                SET "Status" = CASE lower("Status")
                    WHEN 'deployed' THEN 'Deployed'
                    WHEN 'pending' THEN 'Pending'
                    ELSE 'Cancelled'
                END;

                UPDATE "NPC_deployment"
                SET "PositionX" = NULL,
                    "PositionY" = NULL,
                    "PositionMarker" = NULL
                WHERE "Status" = 'Pending';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "NPC_deployment",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldDefaultValue: "deployed");

            migrationBuilder.AlterColumn<float>(
                name: "PositionY",
                table: "NPC_deployment",
                type: "real",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<float>(
                name: "PositionX",
                table: "NPC_deployment",
                type: "real",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<string>(
                name: "PositionMarker",
                table: "NPC_deployment",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_NPC_deployment_MapDifficulty_PositionMarker",
                table: "NPC_deployment",
                columns: new[] { "MapDifficulty", "PositionMarker" },
                unique: true,
                filter: "\"Status\" = 'Deployed'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NPC_deployment_MapDifficulty_PositionMarker",
                table: "NPC_deployment");

            migrationBuilder.Sql(
                """
                UPDATE "NPC_deployment"
                SET "PositionX" = COALESCE("PositionX", 0),
                    "PositionY" = COALESCE("PositionY", 0),
                    "PositionMarker" = COALESCE("PositionMarker", ''),
                    "Status" = CASE "Status"
                        WHEN 'Deployed' THEN 'deployed'
                        ELSE 'removed'
                    END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "NPC_deployment",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "deployed",
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldDefaultValue: "Pending");

            migrationBuilder.AlterColumn<float>(
                name: "PositionY",
                table: "NPC_deployment",
                type: "real",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "PositionX",
                table: "NPC_deployment",
                type: "real",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PositionMarker",
                table: "NPC_deployment",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NPC_deployment_MapDifficulty_PositionMarker",
                table: "NPC_deployment",
                columns: new[] { "MapDifficulty", "PositionMarker" },
                unique: true,
                filter: "\"Status\" = 'deployed'");
        }
    }
}
