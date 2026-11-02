using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class LifespanApiClient
{
    private const string ApiBaseUrl = "https://api.techquestapp.tech/api";
    private static readonly HttpClient Client = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static Task<LifespanResponseDto> GetAsync()
    {
        return SendAsync<LifespanResponseDto>(HttpMethod.Get, "/Lifespan");
    }

    public static Task<LifespanResponseDto> DeductAsync(int amount, string reason)
    {
        return SendAsync<LifespanResponseDto>(HttpMethod.Post, "/Lifespan/deduct", new { amount, reason });
    }

    public static Task<LifespanResponseDto> WalkingRecoveryAsync()
    {
        return SendAsync<LifespanResponseDto>(HttpMethod.Post, "/Lifespan/walking-recovery");
    }

    public static Task<LifespanResponseDto> PurchaseAsync()
    {
        return SendAsync<LifespanResponseDto>(HttpMethod.Post, "/Lifespan/purchase");
    }

    private static async Task<T> SendAsync<T>(HttpMethod method, string endpoint, object body = null)
    {
        if (string.IsNullOrWhiteSpace(UserSession.Token))
            throw new InvalidOperationException("Session token is missing. Please login again.");

        using var request = new HttpRequestMessage(method, ApiBaseUrl + endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", UserSession.Token);
        if (body != null)
        {
            string json = JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using HttpResponseMessage response = await Client.SendAsync(request);
        string responseText = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            string detail = responseText.Trim('"');
            if (string.IsNullOrWhiteSpace(detail))
                detail = response.ReasonPhrase ?? "The server returned an error.";

            throw new InvalidOperationException(
                $"Lifespan API {method} {endpoint} failed with {(int)response.StatusCode}: {detail}");
        }

        return JsonSerializer.Deserialize<T>(responseText, JsonOptions);
    }
}

public class LifespanResponseDto
{
    public int Lifespan { get; set; }
    public int MaximumLifespan { get; set; }
    public int TotalCoins { get; set; }
    public DateTimeOffset? LifespanAutoRecoveryAtUtc { get; set; }
    public DateTimeOffset? LifespanWalkingRecoveryAvailableAtUtc { get; set; }
    public string Message { get; set; } = "";
}
