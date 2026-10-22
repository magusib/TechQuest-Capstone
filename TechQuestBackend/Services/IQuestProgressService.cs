namespace TechQuestBackend.Services
{
    public interface IQuestProgressService
    {
        Task ApplyEventAsync(
            int studentId,
            string eventId,
            string eventType,
            int amount,
            int? npcId = null,
            string? miniGameType = null,
            int? miniGameId = null,
            bool successful = true,
            CancellationToken cancellationToken = default);
    }
}
