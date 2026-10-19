using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechQuestBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionNpcDeployment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeploymentId",
                table: "Question",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Question_DeploymentId",
                table: "Question",
                column: "DeploymentId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Question_NPC_deployment_DeploymentId",
                table: "Question",
                column: "DeploymentId",
                principalTable: "NPC_deployment",
                principalColumn: "DeploymentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(
                """
                INSERT INTO "Question"
                    ("NpcId", "DeploymentId", "MiniGameId", "SubjectName", "Question",
                     "CorrectAnswer", "AnswerTip", "CreatedAt", "CreatedBy")
                SELECT
                    source."NpcId",
                    deployment."DeploymentId",
                    source."MiniGameId",
                    source."SubjectName",
                    source."Question",
                    source."CorrectAnswer",
                    source."AnswerTip",
                    source."CreatedAt",
                    source."CreatedBy"
                FROM "NPC_deployment" AS deployment
                JOIN (
                    SELECT DISTINCT ON ("NpcId")
                        "NpcId", "MiniGameId", "SubjectName", "Question",
                        "CorrectAnswer", "AnswerTip", "CreatedAt", "CreatedBy"
                    FROM "Question"
                    WHERE "NpcId" IS NOT NULL AND "DeploymentId" IS NULL
                    ORDER BY "NpcId", "QuestionId"
                ) AS source ON source."NpcId" = deployment."NpcId"
                WHERE deployment."Status" = 'deployed'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "Question" AS configured
                      WHERE configured."DeploymentId" = deployment."DeploymentId"
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Question_NPC_deployment_DeploymentId",
                table: "Question");

            migrationBuilder.DropIndex(
                name: "IX_Question_DeploymentId",
                table: "Question");

            migrationBuilder.DropColumn(
                name: "DeploymentId",
                table: "Question");
        }
    }
}
