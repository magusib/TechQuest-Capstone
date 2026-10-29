using Godot;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class RoomApiClient
{
	private const string ApiBaseUrl = "https://api.techquestapp.tech/api";
	private static readonly System.Net.Http.HttpClient Client = new();
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public static Task<List<RoomSummaryDto>> GetRoomsAsync()
	{
		return SendAsync<List<RoomSummaryDto>>(HttpMethod.Get, "/MultiplayerRooms");
	}

	public static Task<RoomSummaryDto> CreateRoomAsync(string roomName, string roomCode = "")
	{
		return SendAsync<RoomSummaryDto>(HttpMethod.Post, "/MultiplayerRooms", new { roomName, roomCode });
	}

	public static async Task<string> GenerateRoomCodeAsync()
	{
		RoomCodeDto response = await SendAsync<RoomCodeDto>(HttpMethod.Post, "/MultiplayerRooms/generate-code");
		return response?.RoomCode ?? "";
	}

	public static async Task DeleteRoomAsync(int roomId)
	{
		await SendAsync<object>(HttpMethod.Delete, $"/MultiplayerRooms/{roomId}");
	}

	public static Task<RoomSummaryDto> CloseRoomAsync(int roomId)
	{
		return SendAsync<RoomSummaryDto>(HttpMethod.Post, $"/MultiplayerRooms/{roomId}/close");
	}

	public static Task<RoomSummaryDto> GetRoomAsync(int roomId)
	{
		return SendAsync<RoomSummaryDto>(HttpMethod.Get, $"/MultiplayerRooms/{roomId}");
	}

	public static Task<RoomSummaryDto> GetRoomByCodeAsync(string roomCode)
	{
		return SendAsync<RoomSummaryDto>(HttpMethod.Get, $"/MultiplayerRooms/code/{Uri.EscapeDataString(roomCode)}");
	}

	public static Task<RoomJoinDto> JoinRoomAsync(string roomCode)
	{
		return SendAsync<RoomJoinDto>(HttpMethod.Post, "/MultiplayerRooms/join", new { roomCode });
	}

	public static Task<List<RoomParticipantDto>> GetParticipantsAsync(int roomId)
	{
		return SendAsync<List<RoomParticipantDto>>(HttpMethod.Get, $"/MultiplayerRooms/{roomId}/participants");
	}

	public static Task SaveQuizAsync(int roomId, string fileName, int timerSeconds, List<RoomQuizQuestionDto> questions)
	{
		return SendAsync<object>(HttpMethod.Post, $"/MultiplayerRooms/{roomId}/quiz", new
		{
			fileName,
			timerSeconds,
			questions
		});
	}

	public static Task<RoomQuizDto> GetQuizAsync(int roomId)
	{
		return SendAsync<RoomQuizDto>(HttpMethod.Get, $"/MultiplayerRooms/{roomId}/quiz");
	}

	public static Task SaveResultAsync(int roomId, int score, int totalQuestions, List<RoomAnswerDto> answers)
	{
		return SendAsync<object>(HttpMethod.Post, $"/MultiplayerRooms/{roomId}/results", new
		{
			score,
			totalQuestions,
			answers
		});
	}

	public static Task<RoomResultDto> GetMyResultAsync(int roomId)
	{
		return SendAsync<RoomResultDto>(HttpMethod.Get, $"/MultiplayerRooms/{roomId}/results/me");
	}

	public static Task<List<RoomResultSummaryDto>> GetResultsAsync(int roomId)
	{
		return SendAsync<List<RoomResultSummaryDto>>(HttpMethod.Get, $"/MultiplayerRooms/{roomId}/results");
	}

	private static async Task<T> SendAsync<T>(HttpMethod method, string endpoint, object body = null)
	{
		if (string.IsNullOrWhiteSpace(UserSession.Token))
			throw new InvalidOperationException("Session token is missing. Please login again before using rooms.");

		using var request = new HttpRequestMessage(method, ApiBaseUrl + endpoint);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

		if (!string.IsNullOrWhiteSpace(UserSession.Token))
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", UserSession.Token);

		if (body != null)
		{
			string json = JsonSerializer.Serialize(body, JsonOptions);
			request.Content = new StringContent(json, Encoding.UTF8, "application/json");
		}

		using HttpResponseMessage response = await Client.SendAsync(request);
		string responseText = await response.Content.ReadAsStringAsync();

		if (!response.IsSuccessStatusCode)
			throw new InvalidOperationException(GetApiErrorMessage(responseText, response.ReasonPhrase));

		if (typeof(T) == typeof(object) || string.IsNullOrWhiteSpace(responseText))
			return default;

		return JsonSerializer.Deserialize<T>(responseText, JsonOptions);
	}

	private static string GetApiErrorMessage(string responseText, string fallback)
	{
		if (string.IsNullOrWhiteSpace(responseText))
			return fallback ?? "Request failed.";

		try
		{
			using JsonDocument document = JsonDocument.Parse(responseText);
			JsonElement root = document.RootElement;

			if (root.ValueKind == JsonValueKind.String)
				return root.GetString();

			if (root.TryGetProperty("message", out JsonElement message))
				return message.GetString();

			if (root.TryGetProperty("title", out JsonElement title))
				return title.GetString();
		}
		catch
		{
			return responseText.Trim('"');
		}

		return responseText;
	}
}

public class RoomResultSummaryDto
{
	public int StudentId { get; set; }
	public string StudentName { get; set; } = "";
	public int Score { get; set; }
	public int TotalQuestions { get; set; }
}

public class RoomCodeDto
{
	public string RoomCode { get; set; } = "";
}
