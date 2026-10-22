using Godot;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

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

	public static Task<StudentNpcConfigurationDto> GetStudentConfigurationAsync(int deploymentId)
	{
		return SendAsync<StudentNpcConfigurationDto>(
			HttpMethod.Get,
			"/Npcs/deployments/" + deploymentId + "/student-configuration");
	}

	public static Task<StudentChallengeStartDto> StartChallengeAsync(int deploymentId)
	{
		return SendAsync<StudentChallengeStartDto>(
			HttpMethod.Post,
			"/Npcs/deployments/" + deploymentId + "/challenge-start");
	}

	public static Task<StudentChallengeQuestionDto> GetStudentQuestionAsync(int deploymentId)
	{
		return SendAsync<StudentChallengeQuestionDto>(
			HttpMethod.Get,
			"/Npcs/deployments/" + deploymentId + "/student-question");
	}

	public static Task<QuestionAttemptResultDto> SubmitQuestionAttemptAsync(
		int deploymentId,
		string selectedAnswer)
	{
		return SendAsync<QuestionAttemptResultDto>(
			HttpMethod.Post,
			"/GameResults/question-attempt",
			new QuestionAttemptRequestDto
			{
				DeploymentId = deploymentId,
				SelectedAnswer = selectedAnswer
			});
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
	public int TimerSeconds { get; set; } = 30;
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
	public int TotalPoints { get; set; }
	public string AnswerTip { get; set; } = "";
	public bool AlreadyProcessed { get; set; }
}

public class StudentNpcConfigurationDto
{
	public int NpcId { get; set; }
	public string NpcName { get; set; } = "";
	public int DeploymentId { get; set; }
	public string Difficulty { get; set; } = "";
	public string PositionMarker { get; set; } = "";
	public int QuestionId { get; set; }
	public string SubjectName { get; set; } = "";
	public int? MiniGameId { get; set; }
	public string MiniGameName { get; set; } = "";
	public int TimerSeconds { get; set; }
	public bool AlreadyStarted { get; set; }
	public string ChallengeState { get; set; } = "";
	public string AnswerTip { get; set; } = "";
}

public class StudentChallengeStartDto
{
	public int ChallengeId { get; set; }
	public Guid AttemptId { get; set; }
	public int NpcId { get; set; }
	public int DeploymentId { get; set; }
	public int QuestionId { get; set; }
	public string GameType { get; set; } = "";
	public int TimerSeconds { get; set; }
	public string Difficulty { get; set; } = "";
}

public class StudentChallengeQuestionDto
{
	public int QuestionId { get; set; }
	public int? NpcId { get; set; }
	public int DeploymentId { get; set; }
	public string SubjectName { get; set; } = "";
	public string Question { get; set; } = "";
	public List<string> AnswerChoices { get; set; } = new();
	public int TimerSeconds { get; set; }
	public DateTime QuestionStartedAt { get; set; }
	public int RemainingSeconds { get; set; }
	public string Difficulty { get; set; } = "";
}

public class QuestionAttemptRequestDto
{
	public int DeploymentId { get; set; }
	public string SelectedAnswer { get; set; } = "";
}

public class QuestionAttemptResultDto
{
	public int DeploymentId { get; set; }
	public bool IsCorrect { get; set; }
	public bool TimedOut { get; set; }
	public string SelectedAnswer { get; set; } = "";
	public string CorrectAnswer { get; set; } = "";
	public int CoinsChanged { get; set; }
	public int PointsChanged { get; set; }
	public int TotalCoins { get; set; }
	public int TotalPoints { get; set; }
	public bool Completed { get; set; }
}
