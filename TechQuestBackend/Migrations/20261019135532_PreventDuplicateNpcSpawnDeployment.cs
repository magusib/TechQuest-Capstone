using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechQuestBackend.Migrations
{
    /// <inheritdoc />
    public partial class PreventDuplicateNpcSpawnDeployment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                WITH ranked_deployments AS (
                    SELECT
                        "DeploymentId",
                        ROW_NUMBER() OVER (
                            PARTITION BY "MapDifficulty", "PositionMarker"
                            ORDER BY "DeployedAt", "DeploymentId"
                        ) AS "Position"
                    FROM "NPC_deployment"
                    WHERE lower("Status") = 'deployed'
                )
                UPDATE "NPC_deployment" AS deployment
                SET "Status" = 'removed'
                FROM ranked_deployments AS ranked
                WHERE deployment."DeploymentId" = ranked."DeploymentId"
                  AND ranked."Position" > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_NPC_deployment_MapDifficulty_PositionMarker",
                table: "NPC_deployment",
                columns: new[] { "MapDifficulty", "PositionMarker" },
                unique: true,
                filter: "\"Status\" = 'deployed'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NPC_deployment_MapDifficulty_PositionMarker",
                table: "NPC_deployment");
        }
    }
}
