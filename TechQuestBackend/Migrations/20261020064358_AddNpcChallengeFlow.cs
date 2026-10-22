using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechQuestBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddNpcChallengeFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnswerChoices",
                table: "Question",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "TimerSeconds",
                table: "Question",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "PointsDeducted",
                table: "Game_result",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Npc_challenges",
                columns: table => new
                {
                    ChallengeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    DeploymentId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "MiniGamePending"),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    QuestionStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MiniGameWon = table.Column<bool>(type: "boolean", nullable: true),
                    SelectedAnswer = table.Column<string>(type: "text", nullable: true),
                    QuestionCorrect = table.Column<bool>(type: "boolean", nullable: true),
                    QuestionTimedOut = table.Column<bool>(type: "boolean", nullable: false),
                    CoinsChanged = table.Column<int>(type: "integer", nullable: false),
                    PointsChanged = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Npc_challenges", x => x.ChallengeId);
                    table.CheckConstraint("CK_Npc_challenges_State", "\"State\" IN ('MiniGamePending', 'QuestionReady', 'Completed')");
                    table.ForeignKey(
                        name: "FK_Npc_challenges_NPC_deployment_DeploymentId",
                        column: x => x.DeploymentId,
                        principalTable: "NPC_deployment",
                        principalColumn: "DeploymentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Npc_challenges_Question_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Question",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Npc_challenges_Student_profile_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student_profile",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Game_result_PointsDeducted_NonNegative",
                table: "Game_result",
                sql: "\"PointsDeducted\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Npc_challenges_AttemptId",
                table: "Npc_challenges",
                column: "AttemptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Npc_challenges_DeploymentId",
                table: "Npc_challenges",
                column: "DeploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Npc_challenges_QuestionId",
                table: "Npc_challenges",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Npc_challenges_StudentId_DeploymentId",
                table: "Npc_challenges",
                columns: new[] { "StudentId", "DeploymentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Npc_challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Game_result_PointsDeducted_NonNegative",
                table: "Game_result");

            migrationBuilder.DropColumn(
                name: "AnswerChoices",
                table: "Question");

            migrationBuilder.DropColumn(
                name: "TimerSeconds",
                table: "Question");

            migrationBuilder.DropColumn(
                name: "PointsDeducted",
                table: "Game_result");
        }
    }
}
