using Microsoft.EntityFrameworkCore;
using TechQuestBackend.Data;
using TechQuestBackend.Models;

namespace TechQuestBackend.Services
{
    public sealed class QuestProgressService : IQuestProgressService
    {
        private readonly TechQuestDbContext _db;

        public QuestProgressService(TechQuestDbContext db)
        {
            _db = db;
        }

        public async Task ApplyEventAsync(
            int studentId,
            string eventId,
            string eventType,
            int amount,
            int? npcId = null,
            string? miniGameType = null,
            int? miniGameId = null,
            bool successful = true,
            CancellationToken cancellationToken = default)
        {
            if (studentId <= 0 || string.IsNullOrWhiteSpace(eventId) || amount <= 0)
                return;

            var now = DateTime.UtcNow;
            var quests = await _db.Quests
                .Where(quest => quest.StartDate <= now &&
                    (!quest.EndDate.HasValue || quest.EndDate.Value >= now))
                .ToListAsync(cancellationToken);

            foreach (var quest in quests)
            {
                if (!Matches(quest, eventType, npcId, miniGameType, miniGameId, successful))
                    continue;

                var studentQuest = await _db.StudentQuests
                    .SingleOrDefaultAsync(item =>
                        item.StudentId == studentId && item.QuestId == quest.QuestId,
                        cancellationToken);

                if (studentQuest == null)
                {
                    studentQuest = new StudentQuest
                    {
                        StudentId = studentId,
                        QuestId = quest.QuestId,
                        Status = "in_progress"
                    };
                    _db.StudentQuests.Add(studentQuest);
                }
                else if (string.Equals(studentQuest.Status, "completed", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (await _db.QuestProgressEvents.AnyAsync(item =>
                    item.StudentQuestId == studentQuest.StudentQuestId && item.EventId == eventId,
                    cancellationToken))
                {
                    continue;
                }

                _db.QuestProgressEvents.Add(new QuestProgressEvent
                {
                    StudentQuest = studentQuest,
                    StudentQuestId = studentQuest.StudentQuestId,
                    EventId = eventId,
                    EventType = eventType,
                    Amount = amount,
                    NpcId = npcId,
                    ProcessedAt = now
                });

                long updatedProgress = (long)studentQuest.Progress + amount;
                studentQuest.Progress = (int)Math.Min(Math.Max(1, quest.TargetQuantity), updatedProgress);

                if (studentQuest.Progress >= Math.Max(1, quest.TargetQuantity))
                {
                    studentQuest.Status = "completed";
                    studentQuest.CompletedAt = now;
                }
                else
                {
                    studentQuest.Status = "in_progress";
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        private static bool Matches(
            Quest quest,
            string eventType,
            int? npcId,
            string? miniGameType,
            int? miniGameId,
            bool successful)
        {
            if (!string.Equals(quest.ObjectiveType, eventType, StringComparison.OrdinalIgnoreCase))
                return false;

            if (eventType == "mini_game")
            {
                if (quest.RequireMiniGameSuccess && !successful)
                    return false;
                if (!string.IsNullOrWhiteSpace(quest.RequiredMiniGameType) &&
                    !string.Equals(quest.RequiredMiniGameType, miniGameType, StringComparison.OrdinalIgnoreCase))
                    return false;
                if (quest.RequiredMiniGameId.HasValue && quest.RequiredMiniGameId != miniGameId)
                    return false;
            }

            return true;
        }
    }
}
