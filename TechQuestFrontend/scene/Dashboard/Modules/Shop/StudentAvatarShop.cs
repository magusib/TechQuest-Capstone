using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class StudentAvatarShop : Control
{
	private GridContainer itemGrid;
	private Label coinLabel;
	private Label shopTitle;
	private Panel itemTemplate;
	private string category = "avatar";

	public override void _Ready()
	{
		itemGrid = GetNode<GridContainer>("ShopPanel/ScrollContainer/ItemGrid");
		coinLabel = GetNode<Label>("ShopPanel/CoinLabel");
		shopTitle = GetNode<Label>("ShopPanel/ShopTitle");
		itemTemplate = GetNode<Panel>("ItemPanel");
		ScrollContainer scroll = GetNode<ScrollContainer>("ShopPanel/ScrollContainer");
		scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Auto;
		scroll.VerticalScrollMode = ScrollContainer.ScrollMode.Disabled;

		GetNode<Button>("ShopPanel/CategoryButtons/CharacterButton").Pressed += () => LoadItems("character");
		GetNode<Button>("ShopPanel/CategoryButtons/AvatarButton").Pressed += () => LoadItems("avatar");
		itemTemplate.Visible = false;
		LoadItems(category);
	}

	private async void LoadItems(string selectedCategory)
	{
		category = selectedCategory;
		shopTitle.Text = category == "character" ? "STUDENT CHARACTER SHOP" : "STUDENT AVATAR SHOP";
		ClearItems();

		try
		{
			CoinsDto coins = await ShopApiClient.GetCoinsAsync();
			UserSession.UpdateStudentProfile(coins.TotalCoins, UserSession.Points);
			coinLabel.Text = "Coins: " + coins.TotalCoins;

			List<ShopItemDto> items = await ShopApiClient.GetItemsAsync(category);
			itemGrid.Columns = Math.Max(1, items.Count);
			foreach (ShopItemDto item in items)
				AddItemCard(item);
		}
		catch (Exception error)
		{
			GD.PrintErr("Shop load failed: " + error.Message);
			ShowMessage(error.Message);
		}
	}

	private void AddItemCard(ShopItemDto item)
	{
		Panel card = (Panel)itemTemplate.Duplicate();
		card.Visible = true;
		itemGrid.AddChild(card);

		Label price = card.GetNode<Label>("Label");
		Label name = card.GetNode<Label>("Label2");
		TextureRect image = card.GetNode<TextureRect>("ItemPlaceholder");
		Button purchase = card.GetNode<Button>("PurchaseButton");

		name.Text = item.ItemName;
		price.Text = item.Owned ? "OWNED" : item.PriceCoins + " Coins";
		purchase.Text = item.Owned ? "OWNED" : "PURCHASE";
		purchase.Disabled = item.Owned;
		SetImage(image, item.AvatarItems ?? item.CharacterItems);
		purchase.Pressed += async () => await Purchase(item, purchase, price);
	}

	private async Task Purchase(ShopItemDto item, Button purchase, Label price)
	{
		try
		{
			PurchaseResultDto result = await ShopApiClient.PurchaseAsync(item.ItemId);
			UserSession.UpdateStudentProfile(result.TotalCoins, UserSession.Points);
			coinLabel.Text = "Coins: " + result.TotalCoins;
			purchase.Text = "OWNED";
			purchase.Disabled = true;
			price.Text = "OWNED";
		}
		catch (Exception error)
		{
			GD.PrintErr("Purchase failed: " + error.Message);
			ShowMessage(error.Message);
		}
	}

	private void ClearItems()
	{
		foreach (Node child in itemGrid.GetChildren())
			child.QueueFree();
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
