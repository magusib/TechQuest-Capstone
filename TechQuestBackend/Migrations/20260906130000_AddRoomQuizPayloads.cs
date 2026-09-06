using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TechQuestBackend.Data;

#nullable disable

namespace TechQuestBackend.Migrations
{
    [DbContext(typeof(TechQuestDbContext))]
    [Migration("20260906130000_AddRoomQuizPayloads")]
    public partial class AddRoomQuizPayloads : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoomId",
                table: "Professor_uploads",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionsJson",
                table: "Professor_uploads",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<int>(
                name: "TimerSeconds",
                table: "Professor_uploads",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<string>(
                name: "AnswersJson",
                table: "Room_result",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<int>(
                name: "TotalQuestions",
                table: "Room_result",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Professor_uploads_RoomId",
                table: "Professor_uploads",
                column: "RoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Professor_uploads_Multiplayer_room_RoomId",
                table: "Professor_uploads",
                column: "RoomId",
                principalTable: "Multiplayer_room",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Room_result_TotalQuestions_NonNegative",
                table: "Room_result",
                sql: "\"TotalQuestions\" >= 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Room_result_TotalQuestions_NonNegative",
                table: "Room_result");

            migrationBuilder.DropForeignKey(
                name: "FK_Professor_uploads_Multiplayer_room_RoomId",
                table: "Professor_uploads");

            migrationBuilder.DropIndex(
                name: "IX_Professor_uploads_RoomId",
                table: "Professor_uploads");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "Professor_uploads");

            migrationBuilder.DropColumn(
                name: "QuestionsJson",
                table: "Professor_uploads");

            migrationBuilder.DropColumn(
                name: "TimerSeconds",
                table: "Professor_uploads");

            migrationBuilder.DropColumn(
                name: "AnswersJson",
                table: "Room_result");

            migrationBuilder.DropColumn(
                name: "TotalQuestions",
                table: "Room_result");
        }
    }
}
