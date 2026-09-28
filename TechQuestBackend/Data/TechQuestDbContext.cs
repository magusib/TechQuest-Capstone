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

        public DbSet<MiniGame> MiniGames { get; set; }

        public DbSet<Question> Questions { get; set; }

        public DbSet<GameResult> GameResults { get; set; }

        public DbSet<Leaderboard> Leaderboards { get; set; }

        public DbSet<LeaderboardHistory> LeaderboardHistories { get; set; }

        public DbSet<LeaderboardHistoryEntry> LeaderboardHistoryEntries { get; set; }

        public DbSet<Quest> Quests { get; set; }

        public DbSet<StudentQuest> StudentQuests { get; set; }

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
                entity.HasCheckConstraint("CK_Student_profile_TotalCoins_NonNegative", "\"TotalCoins\" >= 0");
                entity.HasCheckConstraint("CK_Student_profile_TotalPoints_NonNegative", "\"TotalPoints\" >= 0");
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
                entity.ToTable("NPC");
                entity.HasKey(n => n.NpcId);
                entity.Property(n => n.NpcName).HasMaxLength(150).IsRequired();
                entity.Property(n => n.Status).HasMaxLength(30).HasDefaultValue("active");
                entity.Property(n => n.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(n => n.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(n => n.CreatedBy)
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
                entity.Property(q => q.CorrectAnswer).IsRequired();
                entity.Property(q => q.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(q => q.Npc)
                    .WithMany(n => n.Questions)
                    .HasForeignKey(q => q.NpcId)
                    .OnDelete(DeleteBehavior.SetNull);
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
                entity.Property(g => g.PlayedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasCheckConstraint("CK_Game_result_EarnedPoints_NonNegative", "\"EarnedPoints\" >= 0");
                entity.HasCheckConstraint("CK_Game_result_EarnedCoins_NonNegative", "\"EarnedCoins\" >= 0");
                entity.HasOne(g => g.Student)
                    .WithMany(s => s.GameResults)
                    .HasForeignKey(g => g.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(g => g.Question)
                    .WithMany(q => q.GameResults)
                    .HasForeignKey(g => g.QuestionId)
                    .OnDelete(DeleteBehavior.Restrict);
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
                entity.Property(q => q.RewardPoints).HasDefaultValue(0);
                entity.Property(q => q.RewardCoins).HasDefaultValue(0);
                entity.Property(q => q.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasCheckConstraint("CK_Quest_RewardPoints_NonNegative", "\"RewardPoints\" >= 0");
                entity.HasCheckConstraint("CK_Quest_RewardCoins_NonNegative", "\"RewardCoins\" >= 0");
                entity.HasCheckConstraint("CK_Quest_DateRange", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                entity.HasOne(q => q.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(q => q.CreatedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<StudentQuest>(entity =>
            {
                entity.ToTable("Student_Quest");
                entity.HasKey(s => s.StudentQuestId);
                entity.HasIndex(s => new { s.StudentId, s.QuestId }).IsUnique();
                entity.Property(s => s.Status).HasMaxLength(30).HasDefaultValue("not_started");
                entity.HasOne(s => s.Student)
                    .WithMany(p => p.StudentQuests)
                    .HasForeignKey(s => s.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(s => s.Quest)
                    .WithMany(q => q.StudentQuests)
                    .HasForeignKey(s => s.QuestId)
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