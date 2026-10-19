using Godot;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class MiniGameApiClient
{
	private const string ApiBaseUrl = "http://localhost:5000/api";
	private static readonly System.Net.Http.HttpClient Client = new();
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public static async Task<MiniGameAssignmentDto> GetAssignmentAsync(int deploymentId)
	{
		MiniGameAssignmentDto assignment = await SendAsync<MiniGameAssignmentDto>(
			HttpMethod.Get,
			"/Npcs/deployments/" + deploymentId + "/student-game");
		if (assignment == null ||
			assignment.QuestionId <= 0 ||
			assignment.DeploymentId != deploymentId)
			throw new InvalidOperationException("The NPC mini-game assignment was empty.");

		return assignment;
	}

	public static Task<MiniGameAttemptResultDto> SubmitAttemptAsync(
		Guid attemptId,
		int deploymentId,
		int questionId,
		string gameType,
		bool gameWon)
	{
		return SendAsync<MiniGameAttemptResultDto>(
			HttpMethod.Post,
			"/GameResults/mini-game-attempt",
			new MiniGameAttemptRequestDto
			{
				AttemptId = attemptId,
				DeploymentId = deploymentId,
				QuestionId = questionId,
				GameType = gameType,
				GameWon = gameWon
			});
	}

	private static async Task<T> SendAsync<T>(HttpMethod method, string endpoint, object body = null)
	{
		if (string.IsNullOrWhiteSpace(UserSession.Token))
			throw new InvalidOperationException("Session token is missing. Please log in again.");

		using HttpRequestMessage request = new(method, ApiBaseUrl + endpoint);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", UserSession.Token);

		if (body != null)
		{
			string json = JsonSerializer.Serialize(body, JsonOptions);
			request.Content = new StringContent(json, Encoding.UTF8, "application/json");
		}

		using HttpResponseMessage response = await Client.SendAsync(request);
		string responseText = await response.Content.ReadAsStringAsync();
		if (!response.IsSuccessStatusCode)
			throw new InvalidOperationException(
				string.IsNullOrWhiteSpace(responseText)
					? "Mini-game request failed (" + (int)response.StatusCode + ")."
					: responseText.Trim('"'));

		if (string.IsNullOrWhiteSpace(responseText))
			return default;

		return JsonSerializer.Deserialize<T>(responseText, JsonOptions);
	}
}

public class MiniGameAssignmentDto
{
	public int QuestionId { get; set; }
	public int? NpcId { get; set; }
	public int DeploymentId { get; set; }
	public string GameType { get; set; } = "";
	public string GameName { get; set; } = "";
}

public class MiniGameAttemptRequestDto
{
	public Guid AttemptId { get; set; }
	public int DeploymentId { get; set; }
	public int QuestionId { get; set; }
	public string GameType { get; set; } = "";
	public bool GameWon { get; set; }
}

public class MiniGameAttemptResultDto
{
	public Guid AttemptId { get; set; }
	public bool GameWon { get; set; }
	public int CoinsDeducted { get; set; }
	public int TotalCoins { get; set; }
	public string AnswerTip { get; set; } = "";
	public bool AlreadyProcessed { get; set; }
}
