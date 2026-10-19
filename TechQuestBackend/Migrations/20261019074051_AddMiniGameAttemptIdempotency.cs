using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechQuestBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddMiniGameAttemptIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AttemptId",
                table: "Game_result",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CoinsDeducted",
                table: "Game_result",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Game_result_AttemptId",
                table: "Game_result",
                column: "AttemptId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Game_result_CoinsDeducted_NonNegative",
                table: "Game_result",
                sql: "\"CoinsDeducted\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Game_result_AttemptId",
                table: "Game_result");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Game_result_CoinsDeducted_NonNegative",
                table: "Game_result");

            migrationBuilder.DropColumn(
                name: "AttemptId",
                table: "Game_result");

            migrationBuilder.DropColumn(
                name: "CoinsDeducted",
                table: "Game_result");
        }
    }
}
