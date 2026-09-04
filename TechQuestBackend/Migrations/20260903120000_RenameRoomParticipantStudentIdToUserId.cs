using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TechQuestBackend.Data;

#nullable disable

namespace TechQuestBackend.Migrations
{
    [DbContext(typeof(TechQuestDbContext))]
    [Migration("20260903120000_RenameRoomParticipantStudentIdToUserId")]
    public partial class RenameRoomParticipantStudentIdToUserId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StudentId",
                table: "Room_participants",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Room_participants_StudentId",
                table: "Room_participants",
                newName: "IX_Room_participants_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Room_participants_RoomId_StudentId",
                table: "Room_participants",
                newName: "IX_Room_participants_RoomId_UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Room_participants_UserId",
                table: "Room_participants",
                newName: "IX_Room_participants_StudentId");

            migrationBuilder.RenameIndex(
                name: "IX_Room_participants_RoomId_UserId",
                table: "Room_participants",
                newName: "IX_Room_participants_RoomId_StudentId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Room_participants",
                newName: "StudentId");
        }
    }
}
