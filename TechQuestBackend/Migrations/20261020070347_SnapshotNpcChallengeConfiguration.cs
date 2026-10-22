using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechQuestBackend.Migrations
{
    /// <inheritdoc />
    public partial class SnapshotNpcChallengeConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnswerChoices",
                table: "Npc_challenges",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AnswerTip",
                table: "Npc_challenges",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CorrectAnswer",
                table: "Npc_challenges",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                table: "Npc_challenges",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MiniGameType",
                table: "Npc_challenges",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NpcId",
                table: "Npc_challenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NpcName",
                table: "Npc_challenges",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QuestionText",
                table: "Npc_challenges",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubjectName",
                table: "Npc_challenges",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TimerSeconds",
                table: "Npc_challenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnswerChoices",
                table: "Npc_challenges");

            migrationBuilder.DropColumn(
                name: "AnswerTip",
                table: "Npc_challenges");

            migrationBuilder.DropColumn(
                name: "CorrectAnswer",
                table: "Npc_challenges");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Npc_challenges");

            migrationBuilder.DropColumn(
                name: "MiniGameType",
                table: "Npc_challenges");

            migrationBuilder.DropColumn(
                name: "NpcId",
                table: "Npc_challenges");

            migrationBuilder.DropColumn(
                name: "NpcName",
                table: "Npc_challenges");

            migrationBuilder.DropColumn(
                name: "QuestionText",
                table: "Npc_challenges");

            migrationBuilder.DropColumn(
                name: "SubjectName",
                table: "Npc_challenges");

            migrationBuilder.DropColumn(
                name: "TimerSeconds",
                table: "Npc_challenges");
        }
    }
}
