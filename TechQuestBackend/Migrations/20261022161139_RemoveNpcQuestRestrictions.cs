using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TechQuestBackend.Data;

#nullable disable

namespace TechQuestBackend.Migrations
{
    [DbContext(typeof(TechQuestDbContext))]
    [Migration("20261022161139_RemoveNpcQuestRestrictions")]
    public partial class RemoveNpcQuestRestrictions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quest_progress_events_StudentQuestId_NpcId",
                table: "Quest_progress_events");

            migrationBuilder.DropColumn(
                name: "RequireUniqueNpcs",
                table: "Quest");

            migrationBuilder.DropColumn(
                name: "RequiredNpcId",
                table: "Quest");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequireUniqueNpcs",
                table: "Quest",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RequiredNpcId",
                table: "Quest",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quest_progress_events_StudentQuestId_NpcId",
                table: "Quest_progress_events",
                columns: new[] { "StudentQuestId", "NpcId" },
                unique: true,
                filter: "\"NpcId\" IS NOT NULL");
        }
    }
}
