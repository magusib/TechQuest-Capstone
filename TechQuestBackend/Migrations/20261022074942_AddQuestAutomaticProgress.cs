using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechQuestBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestAutomaticProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Progress",
                table: "Student_Quest",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ObjectiveType",
                table: "Quest",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<bool>(
                name: "RequireMiniGameSuccess",
                table: "Quest",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireUniqueNpcs",
                table: "Quest",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RequiredMiniGameType",
                table: "Quest",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredNpcId",
                table: "Quest",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetQuantity",
                table: "Quest",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Quest_progress_events",
                columns: table => new
                {
                    QuestProgressEventId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentQuestId = table.Column<int>(type: "integer", nullable: false),
                    EventId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EventType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    NpcId = table.Column<int>(type: "integer", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quest_progress_events", x => x.QuestProgressEventId);
                    table.ForeignKey(
                        name: "FK_Quest_progress_events_Student_Quest_StudentQuestId",
                        column: x => x.StudentQuestId,
                        principalTable: "Student_Quest",
                        principalColumn: "StudentQuestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Student_Quest_Progress_NonNegative",
                table: "Student_Quest",
                sql: "\"Progress\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Quest_TargetQuantity_Positive",
                table: "Quest",
                sql: "\"TargetQuantity\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Quest_progress_events_StudentQuestId_EventId",
                table: "Quest_progress_events",
                columns: new[] { "StudentQuestId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quest_progress_events_StudentQuestId_NpcId",
                table: "Quest_progress_events",
                columns: new[] { "StudentQuestId", "NpcId" },
                unique: true,
                filter: "\"NpcId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Quest_progress_events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Student_Quest_Progress_NonNegative",
                table: "Student_Quest");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Quest_TargetQuantity_Positive",
                table: "Quest");

            migrationBuilder.DropColumn(
                name: "Progress",
                table: "Student_Quest");

            migrationBuilder.DropColumn(
                name: "ObjectiveType",
                table: "Quest");

            migrationBuilder.DropColumn(
                name: "RequireMiniGameSuccess",
                table: "Quest");

            migrationBuilder.DropColumn(
                name: "RequireUniqueNpcs",
                table: "Quest");

            migrationBuilder.DropColumn(
                name: "RequiredMiniGameType",
                table: "Quest");

            migrationBuilder.DropColumn(
                name: "RequiredNpcId",
                table: "Quest");

            migrationBuilder.DropColumn(
                name: "TargetQuantity",
                table: "Quest");
        }
    }
}
