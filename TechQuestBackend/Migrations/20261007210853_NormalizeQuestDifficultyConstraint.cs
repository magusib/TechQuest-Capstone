using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechQuestBackend.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeQuestDifficultyConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Quest\" DROP CONSTRAINT IF EXISTS \"CK_Quest_Difficulty\";");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Quest_Difficulty",
                table: "Quest",
                sql: "lower(btrim(\"Difficulty\")) IN ('easy', 'medium', 'hard')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Quest_Difficulty",
                table: "Quest");
        }
    }
}
