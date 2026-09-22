using Godot;
using System;
using System.Collections.Generic;

public partial class ProfessorShop : Control
{
	private Button backButton;
	private GridContainer itemGrid;
	private Panel itemTemplate;
	private string category = "character";

	public override void _Ready()
	{
		backButton = GetNode<Button>("BackButton");
		backButton.Pressed += OnBackPressed;
		itemGrid = GetNode<GridContainer>("ShopPanel/ScrollContainer/ItemGrid");
		itemTemplate = GetNode<Panel>("ItemPanel");
		GetNode<Button>("ShopPanel/CategoryButtons/CharacterButton").Pressed += () => LoadItems("character");
		GetNode<Button>("ShopPanel/CategoryButtons/AvatarButton").Pressed += () => LoadItems("avatar");
		itemTemplate.Visible = false;
		LoadItems(category);
	}

	private async void LoadItems(string selectedCategory)
	{
		category = selectedCategory;
		GetNode<Label>("ShopPanel/ShopTitle").Text = category == "character" ? "PROFESSOR CHARACTER SHOP" : "PROFESSOR AVATAR SHOP";
		foreach (Node child in itemGrid.GetChildren())
			child.QueueFree();

		try
		{
			List<ShopItemDto> items = await ShopApiClient.GetItemsAsync(category);
			itemGrid.Columns = 3;
			foreach (ShopItemDto item in items)
			{
				Panel card = (Panel)itemTemplate.Duplicate();
				card.Visible = true;
				card.CustomMinimumSize = itemTemplate.Size;
				itemGrid.AddChild(card);
				card.GetNode<Label>("Label").Text = item.PriceCoins + " Coins";
				card.GetNode<Label>("Label2").Text = item.ItemName;
				string imagePath = category == "avatar" ? item.AvatarItems : item.CharacterItems;
				SetImage(card.GetNode<TextureRect>("ItemPlaceholder"), imagePath);
			}
		}
		catch (Exception error)
		{
			GD.PrintErr("Professor shop load failed: " + error.Message);
			ShowMessage(error.Message);
		}
	}

	private static void SetImage(TextureRect image, string path)
	{
		image.Texture = ShopApiClient.LoadShopImage(path);
	}

	private void ShowMessage(string message)
	{
		AcceptDialog dialog = new AcceptDialog { DialogText = message };
		AddChild(dialog);
		dialog.PopupCentered();
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/UserDashboard/Professor Dashboard/ProfessorDashboard.tscn");
	}
}
