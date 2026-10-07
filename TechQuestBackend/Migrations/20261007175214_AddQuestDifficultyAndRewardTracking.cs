using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechQuestBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestDifficultyAndRewardTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Student_Quest\" ADD COLUMN IF NOT EXISTS \"ClaimedAt\" timestamp with time zone NULL;");

            migrationBuilder.Sql(
                "ALTER TABLE \"Student_Quest\" ADD COLUMN IF NOT EXISTS \"RewardClaimed\" boolean NOT NULL DEFAULT FALSE;");

            migrationBuilder.Sql(
                "ALTER TABLE \"Quest\" ADD COLUMN IF NOT EXISTS \"Difficulty\" character varying(20) NOT NULL DEFAULT 'Easy';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClaimedAt",
                table: "Student_Quest");

            migrationBuilder.DropColumn(
                name: "RewardClaimed",
                table: "Student_Quest");

        }
    }
}
