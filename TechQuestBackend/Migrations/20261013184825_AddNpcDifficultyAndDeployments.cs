using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechQuestBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddNpcDifficultyAndDeployments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                table: "NPC",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Easy");

            migrationBuilder.CreateTable(
                name: "NPC_deployment",
                columns: table => new
                {
                    DeploymentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NpcId = table.Column<int>(type: "integer", nullable: false),
                    MapDifficulty = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PositionX = table.Column<float>(type: "real", nullable: false),
                    PositionY = table.Column<float>(type: "real", nullable: false),
                    PositionMarker = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "deployed"),
                    DeployedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeployedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NPC_deployment", x => x.DeploymentId);
                    table.CheckConstraint("CK_NPC_deployment_Difficulty", "lower(btrim(\"MapDifficulty\")) IN ('easy', 'medium', 'hard')");
                    table.ForeignKey(
                        name: "FK_NPC_deployment_NPC_NpcId",
                        column: x => x.NpcId,
                        principalTable: "NPC",
                        principalColumn: "NpcId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NPC_deployment_Users_DeployedBy",
                        column: x => x.DeployedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_NPC_Difficulty",
                table: "NPC",
                sql: "lower(btrim(\"Difficulty\")) IN ('easy', 'medium', 'hard')");

            migrationBuilder.CreateIndex(
                name: "IX_NPC_deployment_DeployedBy",
                table: "NPC_deployment",
                column: "DeployedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NPC_deployment_NpcId",
                table: "NPC_deployment",
                column: "NpcId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NPC_deployment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NPC_Difficulty",
                table: "NPC");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "NPC");
        }
    }
}
