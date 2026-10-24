using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class ShopApiClient
{
    private const string ApiBaseUrl = "https://techquest-capstone.onrender.com/api";
    private static readonly System.Net.Http.HttpClient Client = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static Task<List<ShopItemDto>> GetItemsAsync(string category)
    {
        return SendAsync<List<ShopItemDto>>(HttpMethod.Get, "/Shop/items?category=" + category);
    }

    public static Task<CoinsDto> GetCoinsAsync()
    {
        return SendAsync<CoinsDto>(HttpMethod.Get, "/Shop/coins");
    }

    public static Task<PurchaseResultDto> PurchaseAsync(int itemId)
    {
        return SendAsync<PurchaseResultDto>(HttpMethod.Post, "/Shop/purchase", new { itemId });
    }

    public static Task<List<ShopItemDto>> GetOwnedAsync(string category)
    {
        return SendAsync<List<ShopItemDto>>(HttpMethod.Get, "/Shop/owned?category=" + category);
    }

    public static Task<SelectedCharacterDto> GetSelectedCharacterAsync()
    {
        return SendAsync<SelectedCharacterDto>(HttpMethod.Get, "/Character/selected");
    }

    public static Task<ShopItemDto> AddItemAsync(string category, string itemName, int priceCoins, string imagePath)
    {
        return SendAsync<ShopItemDto>(HttpMethod.Post, "/Shop/items", new
        {
            category,
            itemName,
            priceCoins,
            imagePath
        });
    }

    public static Task<ShopItemDto> UpdateItemAsync(int itemId, string category, string itemName, int priceCoins, string imagePath)
    {
        return SendAsync<ShopItemDto>(HttpMethod.Put, "/Shop/items/" + itemId, new
        {
            category,
            itemName,
            priceCoins,
            imagePath
        });
    }

    public static Task<ShopItemDto> UpdateStorylineAsync(int itemId, string storyline)
    {
        return SendAsync<ShopItemDto>(HttpMethod.Put, "/Shop/character/" + itemId + "/storyline", new
        {
            storyline
        });
    }

    public static Task DeleteItemAsync(int itemId)
    {
        return SendAsync<object>(HttpMethod.Delete, "/Shop/items/" + itemId);
    }

    public static string CopyShopImageToProject(string category, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            throw new InvalidOperationException("The selected image could not be found.");

        string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
            throw new InvalidOperationException("Invalid image format. Use .png, .jpg, or .jpeg.");

        string folderName = category == "avatar" ? "Avatars" : "Characters";
        string projectFolder = ProjectSettings.GlobalizePath("res://Items/" + folderName);
        Directory.CreateDirectory(projectFolder);

        string fileName = Path.GetFileNameWithoutExtension(sourcePath);
        string destinationName = fileName + extension;
        string destinationPath = Path.Combine(projectFolder, destinationName);
        int number = 1;

        while (File.Exists(destinationPath))
        {
            destinationName = fileName + "_" + number + extension;
            destinationPath = Path.Combine(projectFolder, destinationName);
            number++;
        }

        File.Copy(sourcePath, destinationPath, false);
        return "res://Items/" + folderName + "/" + destinationName;
    }

    public static void DeleteCopiedShopImage(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath) || !projectPath.StartsWith("res://"))
            return;

        string physicalPath = ProjectSettings.GlobalizePath(projectPath);
        if (File.Exists(physicalPath))
            File.Delete(physicalPath);
    }

    public static Task<EquipResultDto> EquipAvatarAsync(int itemId)
    {
        return SendAsync<EquipResultDto>(HttpMethod.Post, "/Shop/equip/avatar", new { itemId });
    }

    public static Task<EquipResultDto> EquipCharacterAsync(int itemId)
    {
        return SendAsync<EquipResultDto>(HttpMethod.Post, "/Character/select", new { itemId });
    }

    public static Texture2D LoadShopImage(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (ResourceLoader.Exists(path))
            return GD.Load<Texture2D>(path);

        string physicalPath = path.StartsWith("res://")
            ? ProjectSettings.GlobalizePath(path)
            : path;

        if (!File.Exists(physicalPath))
            return null;

        Image image = Image.LoadFromFile(physicalPath);
        return image == null ? null : ImageTexture.CreateFromImage(image);
    }

    private static async Task<T> SendAsync<T>(HttpMethod method, string endpoint, object body = null)
    {
        if (string.IsNullOrWhiteSpace(UserSession.Token))
            throw new InvalidOperationException("Session token is missing. Please login again.");

        using var request = new HttpRequestMessage(method, ApiBaseUrl + endpoint);
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
            throw new InvalidOperationException(GetError(responseText, response.ReasonPhrase));

        if (string.IsNullOrWhiteSpace(responseText))
            return default;

        return JsonSerializer.Deserialize<T>(responseText, JsonOptions);
    }

    private static string GetError(string responseText, string fallback)
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

public class ShopItemDto
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public string AvatarItems { get; set; }
    public string CharacterItems { get; set; }
    public string Storyline { get; set; }
    public int PriceCoins { get; set; }
    public bool Owned { get; set; }
}

public class CoinsDto
{
    public int TotalCoins { get; set; }
}

public class PurchaseResultDto
{
    public string Message { get; set; } = "";
    public int TotalCoins { get; set; }
}

public class EquipResultDto
{
    public int ItemId { get; set; }
    public string Avatar { get; set; }
}

public class SelectedCharacterDto
{
    public int ItemId { get; set; }
    public ShopItemDto Item { get; set; }
}
