using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class SelectedCharacterService
{
    public static async Task<string> GetOwnedSelectedScenePathAsync()
    {
        List<ShopItemDto> ownedCharacters = await ShopApiClient.GetOwnedAsync("character");
        if (ownedCharacters == null || ownedCharacters.Count == 0)
            return "";

        SelectedCharacterDto selectedCharacter = null;
        try
        {
            selectedCharacter = await ShopApiClient.GetSelectedCharacterAsync();
        }
        catch
        {
            selectedCharacter = null;
        }

        ShopItemDto ownedSelectedCharacter = null;
        if (selectedCharacter != null)
        {
            foreach (ShopItemDto item in ownedCharacters)
            {
                if (item.ItemId == selectedCharacter.ItemId)
                {
                    ownedSelectedCharacter = item;
                    break;
                }
            }
        }

        if (ownedSelectedCharacter == null)
        {
            foreach (ShopItemDto item in ownedCharacters)
            {
                if (GetCharacterNumber(item) >= 1 && GetCharacterNumber(item) <= 10)
                {
                    ownedSelectedCharacter = item;
                    try
                    {
                        await ShopApiClient.EquipCharacterAsync(item.ItemId);
                    }
                    catch (Exception error)
                    {
                        GD.PrintErr("Could not save fallback selected character: " + error.Message);
                    }
                    break;
                }
            }
        }

        if (ownedSelectedCharacter == null)
            return "";

        int characterNumber = GetCharacterNumber(ownedSelectedCharacter);
        if (characterNumber < 1 || characterNumber > 10)
            return "";

        return $"res://scene/AdventureGame/Character/Character{characterNumber}.tscn";
    }

    private static int GetCharacterNumber(ShopItemDto item)
    {
        int number = GetNumber(item.ItemName);
        if (number >= 1 && number <= 10)
            return number;

        return GetNumber(item.CharacterItems);
    }

    private static int GetNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        string digits = "";
        foreach (char character in value)
        {
            if (char.IsDigit(character))
                digits += character;
        }

        return int.TryParse(digits, out int number) ? number : 0;
    }
}