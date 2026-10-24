using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

public static class AccountApiClient
{
	private const string ApiBaseUrl = "https://techquest-capstone.onrender.com/api";
	private const string ServerBaseUrl = "https://techquest-capstone.onrender.com";
	private static readonly System.Net.Http.HttpClient Client = new();
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public static async Task<AccountProfileDto> GetProfileAsync()
	{
		using var request = CreateRequest(HttpMethod.Get, "/Account/me");
		using HttpResponseMessage response = await Client.SendAsync(request);
		string responseText = await response.Content.ReadAsStringAsync();
		if (!response.IsSuccessStatusCode)
			throw new InvalidOperationException(responseText);

		return JsonSerializer.Deserialize<AccountProfileDto>(responseText, JsonOptions)
			?? throw new InvalidOperationException("Profile response was empty.");
	}

	public static async Task<string> UploadAvatarAsync(string filePath)
	{
		if (!File.Exists(filePath))
			throw new InvalidOperationException("The selected image could not be found.");

		using var request = CreateRequest(HttpMethod.Post, "/Account/avatar");
		using var form = new MultipartFormDataContent();
		using var image = new StreamContent(File.OpenRead(filePath));
		image.Headers.ContentType = new MediaTypeHeaderValue(Path.GetExtension(filePath).ToLowerInvariant() == ".png"
			? "image/png"
			: "image/jpeg");
		form.Add(image, "image", Path.GetFileName(filePath));
		request.Content = form;

		using HttpResponseMessage response = await Client.SendAsync(request);
		string responseText = await response.Content.ReadAsStringAsync();
		if (!response.IsSuccessStatusCode)
		{
			if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
				throw new InvalidOperationException("Your login session no longer matches an account on the server. Please log out and log in again.");

			string message = string.IsNullOrWhiteSpace(responseText)
				? $"Avatar upload failed ({(int)response.StatusCode} {response.ReasonPhrase})."
				: responseText;
			throw new InvalidOperationException(message);
		}

		return JsonSerializer.Deserialize<AvatarUploadDto>(responseText, JsonOptions)?.Avatar
			?? throw new InvalidOperationException("Avatar upload response was empty.");
	}

	public static async Task<List<ManagedUserDto>> GetUsersAsync()
	{
		using var request = CreateRequest(HttpMethod.Get, "/Account/users");
		using HttpResponseMessage response = await Client.SendAsync(request);
		string responseText = await response.Content.ReadAsStringAsync();
		if (!response.IsSuccessStatusCode)
			throw new InvalidOperationException(responseText);

		return JsonSerializer.Deserialize<List<ManagedUserDto>>(responseText, JsonOptions)
			?? new List<ManagedUserDto>();
	}

	public static async Task DeleteUserAsync(int userId)
	{
		using var request = CreateRequest(HttpMethod.Delete, $"/Account/users/{userId}");
		using HttpResponseMessage response = await Client.SendAsync(request);
		if (!response.IsSuccessStatusCode)
		{
			string responseText = await response.Content.ReadAsStringAsync();
			throw new InvalidOperationException(responseText);
		}
	}

	public static async Task<Texture2D> LoadAvatarAsync(string avatarPath)
	{
		if (string.IsNullOrWhiteSpace(avatarPath))
			return null;

		if (ResourceLoader.Exists(avatarPath) || File.Exists(avatarPath))
			return ShopApiClient.LoadShopImage(avatarPath);

		string url = avatarPath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
			? avatarPath
			: ServerBaseUrl + "/" + avatarPath.TrimStart('/');
		byte[] imageBytes = await Client.GetByteArrayAsync(url);
		string extension = Path.GetExtension(avatarPath.Split('?')[0]).ToLowerInvariant();
		Image image = new();
		Error error = extension == ".png"
			? image.LoadPngFromBuffer(imageBytes)
			: image.LoadJpgFromBuffer(imageBytes);
		return error == Error.Ok ? ImageTexture.CreateFromImage(image) : null;
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

public class AccountProfileDto
{
	public string FirstName { get; set; } = "";
	public string LastName { get; set; } = "";
	public string Email { get; set; } = "";
	public int? YearLevel { get; set; }
	public string Avatar { get; set; } = "";
}

public class AvatarUploadDto
{
	public string Avatar { get; set; } = "";
}

public class ManagedUserDto
{
	public int Id { get; set; }
	public string FirstName { get; set; } = "";
	public string LastName { get; set; } = "";
	public string Email { get; set; } = "";
	public string Role { get; set; } = "";
	public int? YearLevel { get; set; }
	public string FullName => $"{FirstName} {LastName}".Trim();
}
