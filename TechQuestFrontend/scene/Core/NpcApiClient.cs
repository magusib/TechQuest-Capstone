using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class NpcApiClient
{
	private const string ApiBaseUrl = "https://api.techquestapp.tech/api";
	private static readonly System.Net.Http.HttpClient Client = new();
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public static async Task<List<NpcDto>> GetNpcsAsync(string difficulty)
	{
		using var request = CreateRequest(HttpMethod.Get, $"/Npcs?difficulty={Uri.EscapeDataString(difficulty)}");
		using HttpResponseMessage response = await Client.SendAsync(request);
		string responseText = await response.Content.ReadAsStringAsync();
		EnsureSuccess(response, responseText);

		return JsonSerializer.Deserialize<List<NpcDto>>(responseText, JsonOptions) ?? new List<NpcDto>();
	}

	public static async Task<List<MiniGameDto>> GetMiniGamesAsync()
	{
		using var request = CreateRequest(HttpMethod.Get, "/MiniGames");
		using HttpResponseMessage response = await Client.SendAsync(request);
		string responseText = await response.Content.ReadAsStringAsync();
		EnsureSuccess(response, responseText);
		return JsonSerializer.Deserialize<List<MiniGameDto>>(responseText, JsonOptions) ?? new List<MiniGameDto>();
	}

	public static async Task<NpcConfigurationDto> GetNpcConfigurationAsync(int npcId)
	{
		using var request = CreateRequest(HttpMethod.Get, $"/Npcs/{npcId}/configuration");
		using HttpResponseMessage response = await Client.SendAsync(request);
		string responseText = await response.Content.ReadAsStringAsync();
		EnsureSuccess(response, responseText);
		return JsonSerializer.Deserialize<NpcConfigurationDto>(responseText, JsonOptions)
			?? throw new InvalidOperationException("NPC configuration response was empty.");
	}

	public static async Task SaveNpcConfigurationAsync(NpcConfigurationDto configuration)
	{
		string json = JsonSerializer.Serialize(configuration, JsonOptions);
		using var request = CreateRequest(HttpMethod.Put, $"/Npcs/{configuration.NpcId}/configuration");
		request.Content = new StringContent(json, Encoding.UTF8, "application/json");
		using HttpResponseMessage response = await Client.SendAsync(request);
		string responseText = await response.Content.ReadAsStringAsync();
		EnsureSuccess(response, responseText);
	}

	public static async Task<NpcConfigurationDto> GetDeploymentConfigurationAsync(int deploymentId)
	{
		using var request = CreateRequest(
			HttpMethod.Get,
			$"/Npcs/deployments/{deploymentId}/configuration");
		using HttpResponseMessage response = await Client.SendAsync(request);
		string responseText = await response.Content.ReadAsStringAsync();
		EnsureSuccess(response, responseText);
		return JsonSerializer.Deserialize<NpcConfigurationDto>(responseText, JsonOptions)
			?? throw new InvalidOperationException("NPC deployment configuration response was empty.");
	}

	public static async Task SaveDeploymentConfigurationAsync(
		int deploymentId,
		NpcConfigurationDto configuration)
	{
		string json = JsonSerializer.Serialize(configuration, JsonOptions);
		using var request = CreateRequest(
			HttpMethod.Put,
			$"/Npcs/deployments/{deploymentId}/configuration");
		request.Content = new StringContent(json, Encoding.UTF8, "application/json");
		using HttpResponseMessage response = await Client.SendAsync(request);
		string responseText = await response.Content.ReadAsStringAsync();
		EnsureSuccess(response, responseText);
	}

	public static async Task<string> UploadImageAsync(string filePath)
	{
		if (!File.Exists(filePath))
			throw new InvalidOperationException("The selected image could not be found.");

		string extension = Path.GetExtension(filePath).ToLowerInvariant();
		if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
			throw new InvalidOperationException("NPC image must be PNG, JPG, or JPEG.");

		using var request = CreateRequest(HttpMethod.Post, "/Npcs/image");
		using var form = new MultipartFormDataContent();
		using var image = new StreamContent(File.OpenRead(filePath));
		image.Headers.ContentType = new MediaTypeHeaderValue(extension == ".png" ? "image/png" : "image/jpeg");
		form.Add(image, "image", Path.GetFileName(filePath));
		request.Content = form;

		using HttpResponseMessage response = await Client.SendAsync(request);
		string responseText = await response.Content.ReadAsStringAsync();
		EnsureSuccess(response, responseText);
		return JsonSerializer.Deserialize<NpcImageDto>(responseText, JsonOptions)?.NpcImage
			?? throw new InvalidOperationException("NPC image upload response was empty.");
	}

	public static async Task<NpcDto> SaveNpcAsync(NpcDto npc)
	{
		string json = JsonSerializer.Serialize(npc, JsonOptions);
		bool isNewNpc = npc.NpcId == 0;
		using var request = CreateRequest(
			isNewNpc ? HttpMethod.Post : HttpMethod.Put,
			isNewNpc ? "/Npcs" : $"/Npcs/{npc.NpcId}");
		request.Content = new StringContent(json, Encoding.UTF8, "application/json");

		using HttpResponseMessage response = await Client.SendAsync(request);
		string responseText = await response.Content.ReadAsStringAsync();
		EnsureSuccess(response, responseText);
		return JsonSerializer.Deserialize<NpcDto>(responseText, JsonOptions)
			?? throw new InvalidOperationException("NPC save response was empty.");
	}

	public static async Task DeleteNpcAsync(int npcId)
	{
		using var request = CreateRequest(HttpMethod.Delete, $"/Npcs/{npcId}");
		using HttpResponseMessage response = await Client.SendAsync(request);
		string responseText = await response.Content.ReadAsStringAsync();
		EnsureSuccess(response, responseText);
	}

	public static async Task<NpcDeploymentDto> DeployNpcAsync(
		NpcDto npc,
		string difficulty,
		string markerName,
		Vector2? position)
	{
		var deployment = new NpcDeploymentRequest
		{
			MapDifficulty = difficulty,
			PositionX = position?.X,
			PositionY = position?.Y,
			PositionMarker = markerName
		};
		string json = JsonSerializer.Serialize(deployment, JsonOptions);
		using var request = CreateRequest(HttpMethod.Post, $"/Npcs/{npc.NpcId}/deployments");
		request.Content = new StringContent(json, Encoding.UTF8, "application/json");

		using HttpResponseMessage response = await Client.SendAsync(request);
		string responseText = await response.Content.ReadAsStringAsync();
		EnsureSuccess(response, responseText);
		return JsonSerializer.Deserialize<NpcDeploymentDto>(responseText, JsonOptions)
			?? throw new InvalidOperationException("NPC deployment response was empty.");
	}

	public static async Task<List<NpcDeploymentDto>> ProcessDeploymentQueueAsync(
		string difficulty,
		List<NpcSpawnPointDto> availableSpawnPoints)
	{
		var queueRequest = new NpcDeploymentQueueRequest
		{
			MapDifficulty = difficulty,
			AvailableSpawnPoints = availableSpawnPoints
		};
		string json = JsonSerializer.Serialize(queueRequest, JsonOptions);
		using var request = CreateRequest(HttpMethod.Post, "/Npcs/deployments/process-queue");
		request.Content = new StringContent(json, Encoding.UTF8, "application/json");

		using HttpResponseMessage response = await Client.SendAsync(request);
		string responseText = await response.Content.ReadAsStringAsync();
		EnsureSuccess(response, responseText);
		return JsonSerializer.Deserialize<List<NpcDeploymentDto>>(responseText, JsonOptions)
			?? throw new InvalidOperationException("Deployment queue response was empty.");
	}

	public static async Task UndeployNpcAsync(int deploymentId)
	{
		using var request = CreateRequest(HttpMethod.Delete, $"/Npcs/deployments/{deploymentId}");
		using HttpResponseMessage response = await Client.SendAsync(request);
		string responseText = await response.Content.ReadAsStringAsync();
		EnsureSuccess(response, responseText);
	}

	public static async Task<List<NpcDeploymentDto>> GetDeploymentsAsync(string difficulty)
	{
		using var request = CreateRequest(HttpMethod.Get, $"/Npcs/deployments?difficulty={Uri.EscapeDataString(difficulty)}");
		using HttpResponseMessage response = await Client.SendAsync(request);
		string responseText = await response.Content.ReadAsStringAsync();
		EnsureSuccess(response, responseText);
		return JsonSerializer.Deserialize<List<NpcDeploymentDto>>(responseText, JsonOptions)
			?? new List<NpcDeploymentDto>();
	}

	private static HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
	{
		if (string.IsNullOrWhiteSpace(UserSession.Token))
			throw new InvalidOperationException("Session token is missing. Please login again.");

		var request = new HttpRequestMessage(method, ApiBaseUrl + endpoint);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", UserSession.Token);
		return request;
	}

	private static void EnsureSuccess(HttpResponseMessage response, string responseText)
	{
		if (!response.IsSuccessStatusCode)
			throw new InvalidOperationException(string.IsNullOrWhiteSpace(responseText)
				? $"NPC request failed ({(int)response.StatusCode})."
				: responseText);
	}
}

public class NpcDto
{
	public int NpcId { get; set; }
	public string NpcName { get; set; } = "";
	public string NpcImage { get; set; } = "";
	public string AnimationScenePath { get; set; } = "";
	public string Difficulty { get; set; } = "Easy";
	public string Status { get; set; } = "active";
}

public class NpcImageDto
{
	public string NpcImage { get; set; } = "";
}

public class NpcDeploymentRequest
{
	public string MapDifficulty { get; set; } = "Easy";
	public float? PositionX { get; set; }
	public float? PositionY { get; set; }
	public string PositionMarker { get; set; } = "";
}

public class NpcDeploymentQueueRequest
{
	public string MapDifficulty { get; set; } = "";
	public List<NpcSpawnPointDto> AvailableSpawnPoints { get; set; } = new();
}

public class NpcSpawnPointDto
{
	public string PositionMarker { get; set; } = "";
	public float PositionX { get; set; }
	public float PositionY { get; set; }
}

public class NpcDeploymentDto
{
	public int DeploymentId { get; set; }
	public int NpcId { get; set; }
	public string NpcName { get; set; } = "";
	public string NpcImage { get; set; } = "";
	public string AnimationScenePath { get; set; } = "";
	public string MapDifficulty { get; set; } = "";
	public string Status { get; set; } = "";
	public string PositionMarker { get; set; } = "";
	public float? PositionX { get; set; }
	public float? PositionY { get; set; }
	public DateTime DeployedAt { get; set; }
}

public class MiniGameDto
{
	public int MiniGameId { get; set; }
	public string GameName { get; set; } = "";
	public string GameType { get; set; } = "";
}

public class NpcConfigurationDto
{
	public int NpcId { get; set; }
	public int? DeploymentId { get; set; }
	public string SubjectName { get; set; } = "";
	public int? MiniGameId { get; set; }
	public string MiniGameName { get; set; } = "";
	public string Question { get; set; } = "";
	public List<string> AnswerChoices { get; set; } = new();
	public string CorrectAnswer { get; set; } = "";
	public string AnswerTip { get; set; } = "";
	public int TimerSeconds { get; set; } = 30;
}
