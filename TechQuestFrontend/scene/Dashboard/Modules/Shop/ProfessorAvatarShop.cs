using Godot;
using System;
using System.Collections.Generic;

public partial class ProfessorAvatarShop : Control
{
	private GridContainer itemGrid;
	private Panel itemTemplate;
	private string category = "avatar";

	public override void _Ready()
	{
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
				itemGrid.AddChild(card);
				card.GetNode<Label>("Label").Text = item.PriceCoins + " Coins";
				card.GetNode<Label>("Label2").Text = item.ItemName;
				SetImage(card.GetNode<TextureRect>("ItemPlaceholder"), item.AvatarItems);
			}
		}
		catch (Exception error)
		{
			GD.PrintErr("Professor avatar shop load failed: " + error.Message);
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
}
