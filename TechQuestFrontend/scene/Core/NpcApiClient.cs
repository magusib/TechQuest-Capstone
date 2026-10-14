using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public static class NpcApiClient
{
	private const string ApiBaseUrl = "http://localhost:5000/api";
	private static readonly System.Net.Http.HttpClient Client = new();
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public static List<NpcDto> GetNpcs(string difficulty)
	{
		using var request = CreateRequest(HttpMethod.Get, $"/Npcs?difficulty={Uri.EscapeDataString(difficulty)}");
		using HttpResponseMessage response = Client.Send(request);
		string responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
		EnsureSuccess(response, responseText);

		return JsonSerializer.Deserialize<List<NpcDto>>(responseText, JsonOptions) ?? new List<NpcDto>();
	}

	public static string UploadImage(string filePath)
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

		using HttpResponseMessage response = Client.Send(request);
		string responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
		EnsureSuccess(response, responseText);
		return JsonSerializer.Deserialize<NpcImageDto>(responseText, JsonOptions)?.NpcImage
			?? throw new InvalidOperationException("NPC image upload response was empty.");
	}

	public static NpcDto SaveNpc(NpcDto npc)
	{
		string json = JsonSerializer.Serialize(npc, JsonOptions);
		bool isNewNpc = npc.NpcId == 0;
		using var request = CreateRequest(
			isNewNpc ? HttpMethod.Post : HttpMethod.Put,
			isNewNpc ? "/Npcs" : $"/Npcs/{npc.NpcId}");
		request.Content = new StringContent(json, Encoding.UTF8, "application/json");

		using HttpResponseMessage response = Client.Send(request);
		string responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
		EnsureSuccess(response, responseText);
		return JsonSerializer.Deserialize<NpcDto>(responseText, JsonOptions)
			?? throw new InvalidOperationException("NPC save response was empty.");
	}

	public static void DeleteNpc(int npcId)
	{
		using var request = CreateRequest(HttpMethod.Delete, $"/Npcs/{npcId}");
		using HttpResponseMessage response = Client.Send(request);
		string responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
		EnsureSuccess(response, responseText);
	}

	public static void DeployNpc(NpcDto npc, string difficulty, string markerName, Vector2 position)
	{
		var deployment = new NpcDeploymentRequest
		{
			MapDifficulty = difficulty,
			PositionX = position.X,
			PositionY = position.Y,
			PositionMarker = markerName
		};
		string json = JsonSerializer.Serialize(deployment, JsonOptions);
		using var request = CreateRequest(HttpMethod.Post, $"/Npcs/{npc.NpcId}/deployments");
		request.Content = new StringContent(json, Encoding.UTF8, "application/json");

		using HttpResponseMessage response = Client.Send(request);
		string responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
		EnsureSuccess(response, responseText);
	}

	public static List<NpcDeploymentDto> GetDeployments(string difficulty)
	{
		using var request = CreateRequest(HttpMethod.Get, $"/Npcs/deployments?difficulty={Uri.EscapeDataString(difficulty)}");
		using HttpResponseMessage response = Client.Send(request);
		string responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
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
	public float PositionX { get; set; }
	public float PositionY { get; set; }
	public string PositionMarker { get; set; } = "";
}

public class NpcDeploymentDto
{
	public int NpcId { get; set; }
	public string NpcName { get; set; } = "";
	public string NpcImage { get; set; } = "";
	public float PositionX { get; set; }
	public float PositionY { get; set; }
}
