using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechQuestBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddTechQuestGameTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Mini_games",
                columns: table => new
                {
                    MiniGameId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    GameType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mini_games", x => x.MiniGameId);
                    table.ForeignKey(
                        name: "FK_Mini_games_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Multiplayer_room",
                columns: table => new
                {
                    RoomId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RoomName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "waiting"),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Multiplayer_room", x => x.RoomId);
                    table.ForeignKey(
                        name: "FK_Multiplayer_room_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NPC",
                columns: table => new
                {
                    NpcId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NpcName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NpcImage = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "active"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NPC", x => x.NpcId);
                    table.ForeignKey(
                        name: "FK_NPC_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Professor_uploads",
                columns: table => new
                {
                    UploadId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfessorId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Professor_uploads", x => x.UploadId);
                    table.ForeignKey(
                        name: "FK_Professor_uploads_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Professor_uploads_Users_ProfessorId",
                        column: x => x.ProfessorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Quest",
                columns: table => new
                {
                    QuestId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuestTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RewardPoints = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    RewardCoins = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quest", x => x.QuestId);
                    table.CheckConstraint("CK_Quest_DateRange", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                    table.CheckConstraint("CK_Quest_RewardCoins_NonNegative", "\"RewardCoins\" >= 0");
                    table.CheckConstraint("CK_Quest_RewardPoints_NonNegative", "\"RewardPoints\" >= 0");
                    table.ForeignKey(
                        name: "FK_Quest_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Shop_items",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    AvatarItems = table.Column<string>(type: "text", nullable: true),
                    CharacterItems = table.Column<string>(type: "text", nullable: true),
                    PriceCoins = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shop_items", x => x.ItemId);
                    table.CheckConstraint("CK_Shop_items_PriceCoins_NonNegative", "\"PriceCoins\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Student_profile",
                columns: table => new
                {
                    StudentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TotalCoins = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalPoints = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Student_profile", x => x.StudentId);
                    table.CheckConstraint("CK_Student_profile_TotalCoins_NonNegative", "\"TotalCoins\" >= 0");
                    table.CheckConstraint("CK_Student_profile_TotalPoints_NonNegative", "\"TotalPoints\" >= 0");
                    table.ForeignKey(
                        name: "FK_Student_profile_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Question",
                columns: table => new
                {
                    QuestionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NpcId = table.Column<int>(type: "integer", nullable: true),
                    MiniGameId = table.Column<int>(type: "integer", nullable: true),
                    SubjectName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Question = table.Column<string>(type: "text", nullable: false),
                    CorrectAnswer = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Question", x => x.QuestionId);
                    table.ForeignKey(
                        name: "FK_Question_Mini_games_MiniGameId",
                        column: x => x.MiniGameId,
                        principalTable: "Mini_games",
                        principalColumn: "MiniGameId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Question_NPC_NpcId",
                        column: x => x.NpcId,
                        principalTable: "NPC",
                        principalColumn: "NpcId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Question_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Character",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Character", x => x.CharacterId);
                    table.ForeignKey(
                        name: "FK_Character_Shop_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Shop_items",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Character_Student_profile_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student_profile",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Leaderboard",
                columns: table => new
                {
                    LeaderboardId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    TotalPoints = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    RankPosition = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leaderboard", x => x.LeaderboardId);
                    table.CheckConstraint("CK_Leaderboard_RankPosition_Positive", "\"RankPosition\" IS NULL OR \"RankPosition\" > 0");
                    table.CheckConstraint("CK_Leaderboard_TotalPoints_NonNegative", "\"TotalPoints\" >= 0");
                    table.ForeignKey(
                        name: "FK_Leaderboard_Student_profile_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student_profile",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Room_participants",
                columns: table => new
                {
                    ParticipantsId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Room_participants", x => x.ParticipantsId);
                    table.ForeignKey(
                        name: "FK_Room_participants_Multiplayer_room_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Multiplayer_room",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Room_participants_Student_profile_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student_profile",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Room_result",
                columns: table => new
                {
                    RoomResultId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Room_result", x => x.RoomResultId);
                    table.CheckConstraint("CK_Room_result_Score_NonNegative", "\"Score\" >= 0");
                    table.ForeignKey(
                        name: "FK_Room_result_Multiplayer_room_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Multiplayer_room",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Room_result_Student_profile_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student_profile",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Student_Quest",
                columns: table => new
                {
                    StudentQuestId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    QuestId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "not_started"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Student_Quest", x => x.StudentQuestId);
                    table.ForeignKey(
                        name: "FK_Student_Quest_Quest_QuestId",
                        column: x => x.QuestId,
                        principalTable: "Quest",
                        principalColumn: "QuestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Student_Quest_Student_profile_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student_profile",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "User_inventory",
                columns: table => new
                {
                    InventoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User_inventory", x => x.InventoryId);
                    table.ForeignKey(
                        name: "FK_User_inventory_Shop_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Shop_items",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_User_inventory_Student_profile_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student_profile",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Game_result",
                columns: table => new
                {
                    ResultId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    EarnedPoints = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    EarnedCoins = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    PlayedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Game_result", x => x.ResultId);
                    table.CheckConstraint("CK_Game_result_EarnedCoins_NonNegative", "\"EarnedCoins\" >= 0");
                    table.CheckConstraint("CK_Game_result_EarnedPoints_NonNegative", "\"EarnedPoints\" >= 0");
                    table.ForeignKey(
                        name: "FK_Game_result_Question_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Question",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Game_result_Student_profile_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student_profile",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Character_ItemId",
                table: "Character",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Character_StudentId",
                table: "Character",
                column: "StudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Game_result_QuestionId",
                table: "Game_result",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Game_result_StudentId",
                table: "Game_result",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Leaderboard_StudentId",
                table: "Leaderboard",
                column: "StudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mini_games_CreatedBy",
                table: "Mini_games",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Multiplayer_room_CreatedBy",
                table: "Multiplayer_room",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Multiplayer_room_RoomCode",
                table: "Multiplayer_room",
                column: "RoomCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NPC_CreatedBy",
                table: "NPC",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Professor_uploads_CreatedBy",
                table: "Professor_uploads",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Professor_uploads_ProfessorId",
                table: "Professor_uploads",
                column: "ProfessorId");

            migrationBuilder.CreateIndex(
                name: "IX_Quest_CreatedBy",
                table: "Quest",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Question_CreatedBy",
                table: "Question",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Question_MiniGameId",
                table: "Question",
                column: "MiniGameId");

            migrationBuilder.CreateIndex(
                name: "IX_Question_NpcId",
                table: "Question",
                column: "NpcId");

            migrationBuilder.CreateIndex(
                name: "IX_Room_participants_RoomId_StudentId",
                table: "Room_participants",
                columns: new[] { "RoomId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Room_participants_StudentId",
                table: "Room_participants",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Room_result_RoomId_StudentId",
                table: "Room_result",
                columns: new[] { "RoomId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Room_result_StudentId",
                table: "Room_result",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Student_profile_UserId",
                table: "Student_profile",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Student_Quest_QuestId",
                table: "Student_Quest",
                column: "QuestId");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Quest_StudentId_QuestId",
                table: "Student_Quest",
                columns: new[] { "StudentId", "QuestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_inventory_ItemId",
                table: "User_inventory",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_User_inventory_StudentId_ItemId",
                table: "User_inventory",
                columns: new[] { "StudentId", "ItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Character");

            migrationBuilder.DropTable(
                name: "Game_result");

            migrationBuilder.DropTable(
                name: "Leaderboard");

            migrationBuilder.DropTable(
                name: "Professor_uploads");

            migrationBuilder.DropTable(
                name: "Room_participants");

            migrationBuilder.DropTable(
                name: "Room_result");

            migrationBuilder.DropTable(
                name: "Student_Quest");

            migrationBuilder.DropTable(
                name: "User_inventory");

            migrationBuilder.DropTable(
                name: "Question");

            migrationBuilder.DropTable(
                name: "Multiplayer_room");

            migrationBuilder.DropTable(
                name: "Quest");

            migrationBuilder.DropTable(
                name: "Shop_items");

            migrationBuilder.DropTable(
                name: "Student_profile");

            migrationBuilder.DropTable(
                name: "Mini_games");

            migrationBuilder.DropTable(
                name: "NPC");
        }
    }
}
