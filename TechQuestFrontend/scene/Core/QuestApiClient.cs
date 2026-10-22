#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public static class QuestApiClient
{
    private const string ApiBaseUrl = "http://localhost:5000/api";
    private static readonly System.Net.Http.HttpClient Client = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static List<QuestListDto> GetAvailableQuestsForDifficulty(string difficulty)
    {
        using var request = CreateRequest(HttpMethod.Get, "/Quests");
        using HttpResponseMessage response = Client.Send(request);
        string responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(responseText);

        var quests = JsonSerializer.Deserialize<List<QuestListDto>>(responseText, JsonOptions)
            ?? new List<QuestListDto>();

        var filtered = new List<QuestListDto>();
        foreach (var quest in quests)
        {
            if (string.Equals(quest.Difficulty, difficulty, StringComparison.OrdinalIgnoreCase))
            {
                filtered.Add(quest);
            }
        }

        return filtered;
    }

    public static QuestRewardResultDto ClaimReward(int questId)
    {
        using var request = CreateRequest(HttpMethod.Post, $"/Quests/{questId}/claim-reward");
        using HttpResponseMessage response = Client.Send(request);
        string responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(responseText);

        return JsonSerializer.Deserialize<QuestRewardResultDto>(responseText, JsonOptions)
            ?? throw new InvalidOperationException("Reward claim response was empty.");
    }

    public static QuestListDto CreateQuest(
        string title,
        string description,
        int rewardCoins,
        int rewardPoints,
        string difficulty,
        string objectiveType,
        int targetQuantity,
        string? requiredMiniGameType,
        int? requiredMiniGameId,
        bool requireMiniGameSuccess)
    {
        DateTime startDate = DateTime.UtcNow;
        var questRequest = new QuestCreateRequest
        {
            QuestTitle = title,
            Description = description,
            RewardCoins = rewardCoins,
            RewardPoints = rewardPoints,
            Difficulty = difficulty,
            ObjectiveType = objectiveType,
            TargetQuantity = targetQuantity,
            RequiredMiniGameType = requiredMiniGameType,
            RequiredMiniGameId = requiredMiniGameId,
            RequireMiniGameSuccess = requireMiniGameSuccess,
            StartDate = startDate,
            EndDate = startDate.AddDays(7)
        };

        string json = JsonSerializer.Serialize(questRequest, JsonOptions);
        using var request = CreateRequest(HttpMethod.Post, "/Quests");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = Client.Send(request);
        string responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(responseText);

        return JsonSerializer.Deserialize<QuestListDto>(responseText, JsonOptions)
            ?? throw new InvalidOperationException("Quest creation response was empty.");
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(UserSession.Token))
            throw new InvalidOperationException("Session token is missing. Please login again.");

        var request = new HttpRequestMessage(method, ApiBaseUrl + endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", UserSession.Token);
        return request;
    }
}

public class QuestCreateRequest
{
    public string QuestTitle { get; set; } = "";
    public string Description { get; set; } = "";
    public int RewardCoins { get; set; }
    public string ObjectiveType { get; set; } = "manual";
    public int TargetQuantity { get; set; } = 1;
    public string? RequiredMiniGameType { get; set; }
    public int? RequiredMiniGameId { get; set; }
    public bool RequireMiniGameSuccess { get; set; }
    public int RewardPoints { get; set; }
    public string Difficulty { get; set; } = "Easy";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class QuestListDto
{
    public int QuestId { get; set; }
    public string QuestTitle { get; set; } = "";
    public string Description { get; set; } = "";
    public string Difficulty { get; set; } = "Easy";
    public int RewardPoints { get; set; }
    public int RewardCoins { get; set; }
    public string ObjectiveType { get; set; } = "manual";
    public int TargetQuantity { get; set; } = 1;
    public string? RequiredMiniGameType { get; set; }
    public int? RequiredMiniGameId { get; set; }
    public bool RequireMiniGameSuccess { get; set; }
    public int Progress { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "In Progress";
    public bool RewardClaimed { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public bool IsCompleted { get; set; }
}

public class QuestRewardResultDto
{
    public int StudentQuestId { get; set; }
    public QuestRewardItemDto Rewards { get; set; } = new QuestRewardItemDto();
    public int TotalCoins { get; set; }
    public int TotalPoints { get; set; }
    public bool RewardClaimed { get; set; }
    public DateTime? ClaimedAt { get; set; }
}

public class QuestRewardItemDto
{
    public int Points { get; set; }
    public int Coins { get; set; }
}
