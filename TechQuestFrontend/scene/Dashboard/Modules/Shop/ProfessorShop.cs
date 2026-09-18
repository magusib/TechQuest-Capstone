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
		itemTemplate = GetNode<Panel>("ShopPanel/ItemPanel");
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
			itemGrid.Columns = Math.Max(1, items.Count);
			foreach (ShopItemDto item in items)
			{
				Panel card = (Panel)itemTemplate.Duplicate();
				card.Visible = true;
				itemGrid.AddChild(card);
				card.GetNode<Label>("Label").Text = item.PriceCoins + " Coins";
				card.GetNode<Label>("Label2").Text = item.ItemName;
				SetImage(card.GetNode<TextureRect>("ItemPlaceholder"), item.CharacterItems ?? item.AvatarItems);
			}
		}
		catch (Exception error)
		{
			GD.PrintErr("Professor shop load failed: " + error.Message);
		}
	}

	private static void SetImage(TextureRect image, string path)
	{
		image.Texture = ShopApiClient.LoadShopImage(path);
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/UserDashboard/Professor Dashboard/ProfessorDashboard.tscn");
	}
}
