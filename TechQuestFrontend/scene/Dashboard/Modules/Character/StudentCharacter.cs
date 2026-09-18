using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class StudentCharacter : Control
{
	private HBoxContainer characterContainer;
	private PanelContainer itemTemplate;
	private Window storylinePopup;

	public override void _Ready()
	{
		characterContainer = GetNode<HBoxContainer>("CharacterPanel/MainContainer/CharacterArea/CharacterScroll/CharacterContainer");
		itemTemplate = GetNode<PanelContainer>("StudentCharacterItem");
		storylinePopup = GetNode<Window>("CharacterStorylinePopup");
		GetNode<Button>("CharacterPanel/MainContainer/Header/BackButton").Pressed += OnBackPressed;
		itemTemplate.Visible = false;
		LoadCharacters();
	}

	private async void LoadCharacters()
	{
		try
		{
			List<ShopItemDto> items = await ShopApiClient.GetOwnedAsync("character");
			foreach (ShopItemDto item in items)
				AddCharacter(item);
		}
		catch (Exception error)
		{
			GD.PrintErr("Character load failed: " + error.Message);
		}
	}

	private void AddCharacter(ShopItemDto item)
	{
		PanelContainer card = (PanelContainer)itemTemplate.Duplicate();
		card.Visible = true;
		characterContainer.AddChild(card);
		card.GetNode<Label>("ItemContainer/CharacterName").Text = item.ItemName;
		SetImage(card.GetNode<TextureRect>("ItemContainer/CharacterImage"), item.CharacterItems);
		card.GetNode<Button>("ItemContainer/UseButton").Pressed += async () => await Equip(item.ItemId);
		card.GetNode<Button>("ItemContainer/StorylineButton").Pressed += () => ShowStoryline(item.ItemName);
	}

	private async Task Equip(int itemId)
	{
		try
		{
			await ShopApiClient.EquipCharacterAsync(itemId);
		}
		catch (Exception error)
		{
			GD.PrintErr("Character equip failed: " + error.Message);
		}
	}

	private void ShowStoryline(string name)
	{
		GetNode<Label>("CharacterStorylinePopup/MarginContainer/VBoxContainer/CharacterName").Text = name;
		GetNode<Label>("CharacterStorylinePopup/MarginContainer/VBoxContainer/StoryScroll/StorylineText").Text = "No storyline is stored for this character yet.";
		GetNode<Button>("CharacterStorylinePopup/MarginContainer/VBoxContainer/CloseButton").Pressed += storylinePopup.Hide;
		storylinePopup.PopupCentered();
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/UserDashboard/Student Dashboard/StudentDashboard.tscn");
	}

	private static void SetImage(TextureRect image, string path)
	{
		image.Texture = ShopApiClient.LoadShopImage(path);
	}
}
