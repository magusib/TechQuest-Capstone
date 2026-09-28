using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechQuestBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaderboardHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaderboardHistories",
                columns: table => new
                {
                    LeaderboardHistoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ResetAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardHistories", x => x.LeaderboardHistoryId);
                });

            migrationBuilder.CreateTable(
                name: "LeaderboardHistoryEntries",
                columns: table => new
                {
                    LeaderboardHistoryEntryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeaderboardHistoryId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    YearLevel = table.Column<int>(type: "integer", nullable: true),
                    Avatar = table.Column<string>(type: "text", nullable: true),
                    TotalPoints = table.Column<int>(type: "integer", nullable: false),
                    RankPosition = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardHistoryEntries", x => x.LeaderboardHistoryEntryId);
                    table.CheckConstraint("CK_LeaderboardHistoryEntries_RankPosition_OneToThree", "\"RankPosition\" BETWEEN 1 AND 3");
                    table.CheckConstraint("CK_LeaderboardHistoryEntries_TotalPoints_NonNegative", "\"TotalPoints\" >= 0");
                    table.ForeignKey(
                        name: "FK_LeaderboardHistoryEntries_LeaderboardHistories_LeaderboardH~",
                        column: x => x.LeaderboardHistoryId,
                        principalTable: "LeaderboardHistories",
                        principalColumn: "LeaderboardHistoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardHistoryEntries_LeaderboardHistoryId",
                table: "LeaderboardHistoryEntries",
                column: "LeaderboardHistoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaderboardHistoryEntries");

            migrationBuilder.DropTable(
                name: "LeaderboardHistories");
        }
    }
}
