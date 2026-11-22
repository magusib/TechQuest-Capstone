using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

public static class UserProgressApiClient
{
    private const string ApiBaseUrl = "https://api.techquestapp.tech/api";
    private static readonly HttpClient Client = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task<List<UserProgressItemDto>> GetAsync(string difficulty)
    {
        if (!UserSession.IsStudent || string.IsNullOrWhiteSpace(UserSession.Token))
            throw new InvalidOperationException("Only authenticated students can view User Progress.");

        using var request = new HttpRequestMessage(HttpMethod.Get, ApiBaseUrl + "/UserProgress?difficulty=" + Uri.EscapeDataString(difficulty));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", UserSession.Token);
        using HttpResponseMessage response = await Client.SendAsync(request);
        string body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? "User Progress could not be loaded." : body.Trim('"'));

        return JsonSerializer.Deserialize<List<UserProgressItemDto>>(body, JsonOptions) ?? new List<UserProgressItemDto>();
    }
}

public sealed class UserProgressItemDto
{
    public string Subject { get; set; } = "";
    public string Question { get; set; } = "";
    public string CorrectAnswer { get; set; } = "";
    public string Status { get; set; } = "";
    public int CoinsEarned { get; set; }
    public int PointsEarned { get; set; }
    public DateTime? CompletionDate { get; set; }
}
