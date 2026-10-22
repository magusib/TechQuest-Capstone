using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechQuestBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestMiniGameIdentifier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequiredMiniGameId",
                table: "Quest",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quest_RequiredMiniGameId",
                table: "Quest",
                column: "RequiredMiniGameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quest_Mini_games_RequiredMiniGameId",
                table: "Quest",
                column: "RequiredMiniGameId",
                principalTable: "Mini_games",
                principalColumn: "MiniGameId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quest_Mini_games_RequiredMiniGameId",
                table: "Quest");

            migrationBuilder.DropIndex(
                name: "IX_Quest_RequiredMiniGameId",
                table: "Quest");

            migrationBuilder.DropColumn(
                name: "RequiredMiniGameId",
                table: "Quest");
        }
    }
}
