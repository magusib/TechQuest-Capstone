﻿using Microsoft.EntityFrameworkCore;
using TechQuestBackend.Models;

namespace TechQuestBackend.Data
{
    public class TechQuestDbContext : DbContext
    {
        public TechQuestDbContext(
            DbContextOptions<TechQuestDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<EmailVerification> EmailVerifications { get; set; }

        public DbSet<PasswordReset> PasswordResets { get; set; }

        public DbSet<StudentProfile> StudentProfiles { get; set; }

        public DbSet<Character> Characters { get; set; }

        public DbSet<ShopItem> ShopItems { get; set; }

        public DbSet<UserInventory> UserInventories { get; set; }

        public DbSet<Npc> Npcs { get; set; }

        public DbSet<NpcDeployment> NpcDeployments { get; set; }

        public DbSet<MiniGame> MiniGames { get; set; }

        public DbSet<Question> Questions { get; set; }

        public DbSet<GameResult> GameResults { get; set; }

        public DbSet<NpcChallenge> NpcChallenges { get; set; }

        public DbSet<NpcQuizPurchaseTransaction> NpcQuizPurchaseTransactions { get; set; }

        public DbSet<Leaderboard> Leaderboards { get; set; }

        public DbSet<LeaderboardHistory> LeaderboardHistories { get; set; }

        public DbSet<LeaderboardHistoryEntry> LeaderboardHistoryEntries { get; set; }

        public DbSet<Quest> Quests { get; set; }

        public DbSet<StudentQuest> StudentQuests { get; set; }

        public DbSet<QuestProgressEvent> QuestProgressEvents { get; set; }

        public DbSet<MultiplayerRoom> MultiplayerRooms { get; set; }

        public DbSet<RoomParticipant> RoomParticipants { get; set; }

        public DbSet<ProfessorUpload> ProfessorUploads { get; set; }

        public DbSet<RoomResult> RoomResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Role).HasMaxLength(20);
                entity.Property(u => u.FirstName).HasMaxLength(100);
                entity.Property(u => u.LastName).HasMaxLength(100);
                entity.Property(u => u.Email).HasMaxLength(255);
            });

            modelBuilder.Entity<EmailVerification>(entity =>
            {
                entity.ToTable("EmailVerifications");
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.OtpCode).HasMaxLength(20);
            });

            modelBuilder.Entity<PasswordReset>(entity =>
            {
                entity.ToTable("PasswordResets");
                entity.Property(p => p.Email).HasMaxLength(255);
                entity.Property(p => p.OtpCode).HasMaxLength(20);
            });

            modelBuilder.Entity<StudentProfile>(entity =>
            {
                entity.ToTable("Student_profile");
                entity.HasKey(s => s.StudentId);
                entity.HasIndex(s => s.UserId).IsUnique();
                entity.Property(s => s.TotalCoins).HasDefaultValue(0);
                entity.Property(s => s.TotalPoints).HasDefaultValue(0);
                entity.Property(s => s.Lifespan).HasDefaultValue(100);
                entity.Property(s => s.LifespanAutoRecoveryAt);
                entity.Property(s => s.LifespanWalkRecoveryAt);
                entity.HasCheckConstraint("CK_Student_profile_TotalCoins_NonNegative", "\"TotalCoins\" >= 0");
                entity.HasCheckConstraint("CK_Student_profile_TotalPoints_NonNegative", "\"TotalPoints\" >= 0");
                entity.HasCheckConstraint("CK_Student_profile_Lifespan_Range", "\"Lifespan\" >= 0 AND \"Lifespan\" <= 100");
                entity.HasOne(s => s.User)
                    .WithOne()
                    .HasForeignKey<StudentProfile>(s => s.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Character>(entity =>
            {
                entity.ToTable("Character");
                entity.HasKey(c => c.CharacterId);
                entity.HasIndex(c => c.StudentId).IsUnique();
                entity.HasOne(c => c.Student)
                    .WithOne(s => s.Character)
                    .HasForeignKey<Character>(c => c.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(c => c.Item)
                    .WithMany()
                    .HasForeignKey(c => c.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ShopItem>(entity =>
            {
                entity.ToTable("Shop_items");
                entity.HasKey(s => s.ItemId);
                entity.Property(s => s.ItemName).HasMaxLength(150).IsRequired();
                entity.Property(s => s.Storyline).HasColumnType("text");
                entity.Property(s => s.PriceCoins).HasDefaultValue(0);
                entity.Property(s => s.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasCheckConstraint("CK_Shop_items_PriceCoins_NonNegative", "\"PriceCoins\" >= 0");
            });

            modelBuilder.Entity<UserInventory>(entity =>
            {
                entity.ToTable("User_inventory");
                entity.HasKey(i => i.InventoryId);
                entity.HasIndex(i => new { i.StudentId, i.ItemId }).IsUnique();
                entity.Property(i => i.PurchasedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(i => i.Student)
                    .WithMany(s => s.InventoryItems)
                    .HasForeignKey(i => i.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(i => i.Item)
                    .WithMany(s => s.InventoryItems)
                    .HasForeignKey(i => i.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Npc>(entity =>
            {
                entity.ToTable("NPC", table =>
                    table.HasCheckConstraint("CK_NPC_Difficulty", "lower(btrim(\"Difficulty\")) IN ('easy', 'medium', 'hard')"));
                entity.HasKey(n => n.NpcId);
                entity.Property(n => n.NpcName).HasMaxLength(150).IsRequired();
                entity.Property(n => n.Difficulty).HasMaxLength(20).HasDefaultValue("Easy");
                entity.Property(n => n.Status).HasMaxLength(30).HasDefaultValue("active");
                entity.Property(n => n.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(n => n.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(n => n.CreatedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<NpcDeployment>(entity =>
            {
                entity.ToTable("NPC_deployment", table =>
                {
                    table.HasCheckConstraint("CK_NPC_deployment_Difficulty", "lower(btrim(\"MapDifficulty\")) IN ('easy', 'medium', 'hard')");
                    table.HasCheckConstraint("CK_NPC_deployment_Status", "\"Status\" IN ('Pending', 'Deployed', 'Cancelled')");
                });
                entity.HasKey(d => d.DeploymentId);
                entity.Property(d => d.MapDifficulty).HasMaxLength(20).IsRequired();
                entity.Property(d => d.PositionMarker).HasMaxLength(100);
                entity.Property(d => d.Status).HasMaxLength(30).HasDefaultValue("Pending");
                entity.Property(d => d.DeployedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(d => new { d.MapDifficulty, d.PositionMarker })
                    .IsUnique()
                    .HasFilter("\"Status\" = 'Deployed'");
                entity.HasOne(d => d.Npc)
                    .WithMany(n => n.Deployments)
                    .HasForeignKey(d => d.NpcId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(d => d.DeployedByUser)
                    .WithMany()
                    .HasForeignKey(d => d.DeployedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<MiniGame>(entity =>
            {
                entity.ToTable("Mini_games");
                entity.HasKey(m => m.MiniGameId);
                entity.Property(m => m.GameName).HasMaxLength(150).IsRequired();
                entity.Property(m => m.GameType).HasMaxLength(50).IsRequired();
                entity.Property(m => m.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(m => m.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(m => m.CreatedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Question>(entity =>
            {
                entity.ToTable("Question");
                entity.HasKey(q => q.QuestionId);
                entity.Property(q => q.SubjectName).HasMaxLength(150).IsRequired();
                entity.Property(q => q.QuestionText).HasColumnName("Question").IsRequired();
                entity.Property(q => q.AnswerChoices).HasColumnType("jsonb").HasDefaultValue("[]").IsRequired();
                entity.Property(q => q.CorrectAnswer).IsRequired();
                entity.Property(q => q.AnswerTip).IsRequired();
                entity.Property(q => q.TimerSeconds).HasDefaultValue(30);
                entity.Property(q => q.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(q => q.Npc)
                    .WithMany(n => n.Questions)
                    .HasForeignKey(q => q.NpcId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(q => q.DeploymentId).IsUnique();
                entity.HasOne(q => q.Deployment)
                    .WithMany(d => d.Questions)
                    .HasForeignKey(q => q.DeploymentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(q => q.MiniGame)
                    .WithMany(m => m.Questions)
                    .HasForeignKey(q => q.MiniGameId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(q => q.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(q => q.CreatedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<GameResult>(entity =>
            {
                entity.ToTable("Game_result");
                entity.HasKey(g => g.ResultId);
                entity.Property(g => g.EarnedPoints).HasDefaultValue(0);
                entity.Property(g => g.EarnedCoins).HasDefaultValue(0);
                entity.Property(g => g.CoinsDeducted).HasDefaultValue(0);
                entity.Property(g => g.PointsDeducted).HasDefaultValue(0);
                entity.Property(g => g.PlayedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasCheckConstraint("CK_Game_result_EarnedPoints_NonNegative", "\"EarnedPoints\" >= 0");
                entity.HasCheckConstraint("CK_Game_result_EarnedCoins_NonNegative", "\"EarnedCoins\" >= 0");
                entity.HasCheckConstraint("CK_Game_result_CoinsDeducted_NonNegative", "\"CoinsDeducted\" >= 0");
                entity.HasCheckConstraint("CK_Game_result_PointsDeducted_NonNegative", "\"PointsDeducted\" >= 0");
                entity.HasIndex(g => g.AttemptId).IsUnique();
                entity.HasOne(g => g.Student)
                    .WithMany(s => s.GameResults)
                    .HasForeignKey(g => g.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(g => g.Question)
                    .WithMany(q => q.GameResults)
                    .HasForeignKey(g => g.QuestionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<NpcChallenge>(entity =>
            {
                entity.ToTable("Npc_challenges", table =>
                {
                    table.HasCheckConstraint(
                        "CK_Npc_challenges_State",
                        "\"State\" IN ('MiniGamePending', 'QuestionReady', 'Completed', 'CharacterMiniGamePending', 'CharacterMiniGameCompleted')");
                });
                entity.Property(challenge => challenge.HintPositions)
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'[]'::jsonb");
                entity.HasKey(challenge => challenge.ChallengeId);
                entity.Property(challenge => challenge.State).HasMaxLength(30).HasDefaultValue("MiniGamePending");
                entity.Property(challenge => challenge.NpcName).HasMaxLength(150).IsRequired();
                entity.Property(challenge => challenge.Difficulty).HasMaxLength(20).IsRequired();
                entity.Property(challenge => challenge.SubjectName).HasMaxLength(150).IsRequired();
                entity.Property(challenge => challenge.MiniGameType).HasMaxLength(50).IsRequired();
                entity.Property(challenge => challenge.CharacterName).HasMaxLength(150);
                entity.Property(challenge => challenge.CharacterOutcome).HasMaxLength(20);
                entity.Property(challenge => challenge.AnswerChoices).HasColumnType("jsonb").IsRequired();
                entity.Property(challenge => challenge.StartedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(challenge => new { challenge.StudentId, challenge.DeploymentId }).IsUnique();
                entity.HasIndex(challenge => challenge.AttemptId).IsUnique();
                entity.HasOne(challenge => challenge.Student)
                    .WithMany(student => student.NpcChallenges)
                    .HasForeignKey(challenge => challenge.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(challenge => challenge.Deployment)
                    .WithMany()
                    .HasForeignKey(challenge => challenge.DeploymentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(challenge => challenge.Question)
                    .WithMany(question => question.NpcChallenges)
                    .HasForeignKey(challenge => challenge.QuestionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<NpcQuizPurchaseTransaction>(entity =>
            {
                entity.ToTable("NpcQuizPurchaseTransactions", table =>
                {
                    table.HasCheckConstraint(
                        "CK_NpcQuizPurchaseTransactions_CostCoins",
                        "\"CostCoins\" > 0");
                    table.HasCheckConstraint(
                        "CK_NpcQuizPurchaseTransactions_PurchaseType",
                        "\"PurchaseType\" IN ('extend_time', 'extend_time_hint', 'hint')");
                });
                entity.HasKey(transaction => transaction.TransactionId);
                entity.Property(transaction => transaction.PurchaseType).HasMaxLength(40).IsRequired();
                entity.Property(transaction => transaction.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(transaction => new { transaction.StudentId, transaction.PurchaseId }).IsUnique();
                entity.HasIndex(transaction => transaction.ChallengeId);
                entity.HasOne(transaction => transaction.Student)
                    .WithMany()
                    .HasForeignKey(transaction => transaction.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(transaction => transaction.Challenge)
                    .WithMany()
                    .HasForeignKey(transaction => transaction.ChallengeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Leaderboard>(entity =>
            {
                entity.ToTable("Leaderboard");
                entity.HasKey(l => l.LeaderboardId);
                entity.HasIndex(l => l.StudentId).IsUnique();
                entity.Property(l => l.TotalPoints).HasDefaultValue(0);
                entity.HasCheckConstraint("CK_Leaderboard_TotalPoints_NonNegative", "\"TotalPoints\" >= 0");
                entity.HasCheckConstraint("CK_Leaderboard_RankPosition_Positive", "\"RankPosition\" IS NULL OR \"RankPosition\" > 0");
                entity.HasOne(l => l.Student)
                    .WithMany(s => s.LeaderboardEntries)
                    .HasForeignKey(l => l.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LeaderboardHistory>(entity =>
            {
                entity.ToTable("LeaderboardHistories");
                entity.HasKey(h => h.LeaderboardHistoryId);
                entity.Property(h => h.ResetAt).IsRequired();
            });

            modelBuilder.Entity<LeaderboardHistoryEntry>(entity =>
            {
                entity.ToTable("LeaderboardHistoryEntries", table =>
                {
                    table.HasCheckConstraint("CK_LeaderboardHistoryEntries_TotalPoints_NonNegative", "\"TotalPoints\" >= 0");
                    table.HasCheckConstraint("CK_LeaderboardHistoryEntries_RankPosition_OneToThree", "\"RankPosition\" BETWEEN 1 AND 3");
                });
                entity.HasKey(h => h.LeaderboardHistoryEntryId);
                entity.HasOne(h => h.History)
                    .WithMany(h => h.Entries)
                    .HasForeignKey(h => h.LeaderboardHistoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Quest>(entity =>
            {
                entity.ToTable("Quest");
                entity.HasKey(q => q.QuestId);
                entity.Property(q => q.QuestTitle).HasMaxLength(200).IsRequired();
                entity.Property(q => q.Difficulty).HasMaxLength(20).HasDefaultValue("Easy");
                entity.Property(q => q.ObjectiveType).HasMaxLength(30).HasDefaultValue("manual");
                entity.Property(q => q.TargetQuantity).HasDefaultValue(1);
                entity.Property(q => q.RequiredMiniGameType).HasMaxLength(50);
                entity.Property(q => q.RewardPoints).HasDefaultValue(0);
                entity.Property(q => q.RewardCoins).HasDefaultValue(0);
                entity.Property(q => q.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasCheckConstraint("CK_Quest_RewardPoints_NonNegative", "\"RewardPoints\" >= 0");
                entity.HasCheckConstraint("CK_Quest_RewardCoins_NonNegative", "\"RewardCoins\" >= 0");
                entity.HasCheckConstraint("CK_Quest_TargetQuantity_Positive", "\"TargetQuantity\" > 0");
                entity.HasCheckConstraint("CK_Quest_DateRange", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                entity.HasCheckConstraint("CK_Quest_Difficulty", "lower(btrim(\"Difficulty\")) IN ('easy', 'medium', 'hard')");
                entity.HasOne(q => q.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(q => q.CreatedBy)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(q => q.RequiredMiniGame)
                    .WithMany()
                    .HasForeignKey(q => q.RequiredMiniGameId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StudentQuest>(entity =>
            {
                entity.ToTable("Student_Quest");
                entity.HasKey(s => s.StudentQuestId);
                entity.HasIndex(s => new { s.StudentId, s.QuestId }).IsUnique();
                entity.Property(s => s.Status).HasMaxLength(30).HasDefaultValue("not_started");
                entity.Property(s => s.Progress).HasDefaultValue(0);
                entity.Property(s => s.RewardClaimed).HasDefaultValue(false);
                entity.HasCheckConstraint("CK_Student_Quest_Progress_NonNegative", "\"Progress\" >= 0");
                entity.HasOne(s => s.Student)
                    .WithMany(p => p.StudentQuests)
                    .HasForeignKey(s => s.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(s => s.Quest)
                    .WithMany(q => q.StudentQuests)
                    .HasForeignKey(s => s.QuestId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<QuestProgressEvent>(entity =>
            {
                entity.ToTable("Quest_progress_events");
                entity.HasKey(item => item.QuestProgressEventId);
                entity.Property(item => item.EventId).HasMaxLength(150).IsRequired();
                entity.Property(item => item.EventType).HasMaxLength(30).IsRequired();
                entity.Property(item => item.Amount).IsRequired();
                entity.Property(item => item.ProcessedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(item => new { item.StudentQuestId, item.EventId }).IsUnique();
                entity.HasOne(item => item.StudentQuest)
                    .WithMany(studentQuest => studentQuest.ProgressEvents)
                    .HasForeignKey(item => item.StudentQuestId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MultiplayerRoom>(entity =>
            {
                entity.ToTable("Multiplayer_room");
                entity.HasKey(r => r.RoomId);
                entity.HasIndex(r => r.RoomCode).IsUnique();
                entity.Property(r => r.RoomCode).HasMaxLength(50).IsRequired();
                entity.Property(r => r.RoomName).HasMaxLength(150).IsRequired();
                entity.Property(r => r.Status).HasMaxLength(30).HasDefaultValue("waiting");
                entity.Property(r => r.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(r => r.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(r => r.CreatedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<RoomParticipant>(entity =>
            {
                entity.ToTable("Room_participants");
                entity.HasKey(r => r.ParticipantsId);
                entity.HasIndex(r => new { r.RoomId, r.UserId }).IsUnique();
                entity.Property(r => r.JoinedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(r => r.Room)
                    .WithMany(room => room.Participants)
                    .HasForeignKey(r => r.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(r => r.Student)
                    .WithMany(s => s.RoomParticipants)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProfessorUpload>(entity =>
            {
                entity.ToTable("Professor_uploads");
                entity.HasKey(p => p.UploadId);
                entity.Property(p => p.FileName).HasMaxLength(255).IsRequired();
                entity.Property(p => p.QuestionsJson)
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'[]'::jsonb");
                entity.Property(p => p.TimerSeconds).HasDefaultValue(30);
                entity.Property(p => p.UploadedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(p => p.Professor)
                    .WithMany()
                    .HasForeignKey(p => p.ProfessorId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.Room)
                    .WithMany()
                    .HasForeignKey(p => p.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(p => p.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(p => p.CreatedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<RoomResult>(entity =>
            {
                entity.ToTable("Room_result");
                entity.HasKey(r => r.RoomResultId);
                entity.HasIndex(r => new { r.RoomId, r.StudentId }).IsUnique();
                entity.Property(r => r.Score).HasDefaultValue(0);
                entity.Property(r => r.TotalQuestions).HasDefaultValue(0);
                entity.Property(r => r.AnswersJson)
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'[]'::jsonb");
                entity.Property(r => r.CompletedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasCheckConstraint("CK_Room_result_Score_NonNegative", "\"Score\" >= 0");
                entity.HasCheckConstraint("CK_Room_result_TotalQuestions_NonNegative", "\"TotalQuestions\" >= 0");
                entity.HasOne(r => r.Room)
                    .WithMany(room => room.Results)
                    .HasForeignKey(r => r.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(r => r.Student)
                    .WithMany(s => s.RoomResults)
                    .HasForeignKey(r => r.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
