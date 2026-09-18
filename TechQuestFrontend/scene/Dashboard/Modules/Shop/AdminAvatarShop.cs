using Godot;
using System;
using System.Collections.Generic;

public partial class AdminAvatarShop : Control
{
	private GridContainer itemGrid;
	private Panel itemTemplate;
	private string category = "avatar";
	private string selectedImage = "";
	private LineEdit itemName;
	private LineEdit price;
	private Button imageButton;

	public override void _Ready()
	{
		itemGrid = GetNode<GridContainer>("ShopPanel/ScrollContainer/ItemGrid");
		itemTemplate = GetNode<Panel>("ItemPanel");
		itemName = GetNode<LineEdit>("AddItemPopup/MarginContainer/VBoxContainer/ItemName");
		price = GetNode<LineEdit>("AddItemPopup/MarginContainer/VBoxContainer/Price");
		imageButton = GetNode<Button>("AddItemPopup/MarginContainer/VBoxContainer/ImageButton");
		GetNode<Button>("ShopPanel/AddItemButton").Pressed += OpenAddItem;
		GetNode<Button>("AddItemPopup/MarginContainer/VBoxContainer/Buttons/CancelButton").Pressed += () => GetNode<Window>("AddItemPopup").Hide();
		GetNode<Button>("AddItemPopup/MarginContainer/VBoxContainer/Buttons/AddButton").Pressed += AddItem;
		imageButton.Pressed += SelectImage;
		GetNode<Button>("ShopPanel/AddItemButton/CategoryButtons/CharacterButton").Pressed += () => LoadItems("character");
		GetNode<Button>("ShopPanel/AddItemButton/CategoryButtons/AvatarButton").Pressed += () => LoadItems("avatar");
		itemTemplate.Visible = false;
		LoadItems(category);
	}

	private async void LoadItems(string selectedCategory)
	{
		category = selectedCategory;
		GetNode<Label>("ShopPanel/ShopTitle").Text = category == "character" ? "ADMIN CHARACTER SHOP" : "ADMIN AVATAR SHOP";
		ClearItems();
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
			}
		}
		catch (Exception error)
		{
			GD.PrintErr("Admin avatar shop load failed: " + error.Message);
		}
	}

	private void ClearItems()
	{
		foreach (Node child in itemGrid.GetChildren())
			child.QueueFree();
	}

	private void OpenAddItem()
	{
		itemName.Text = "";
		price.Text = "0";
		selectedImage = "";
		imageButton.Text = "SELECT IMAGE (.png / .jpg / .jpeg)";
		GetNode<Window>("AddItemPopup").PopupCentered();
	}

	private void SelectImage()
	{
		FileDialog dialog = new FileDialog();
		dialog.FileMode = FileDialog.FileModeEnum.OpenFile;
		dialog.Filters = new[] { "*.png ; PNG", "*.jpg ; JPG", "*.jpeg ; JPEG" };
		dialog.FileSelected += path =>
		{
			selectedImage = path;
			imageButton.Text = path.GetFile();
			dialog.QueueFree();
		};
		AddChild(dialog);
		dialog.PopupCenteredRatio();
	}

	private async void AddItem()
	{
		if (string.IsNullOrWhiteSpace(itemName.Text) || !int.TryParse(price.Text, out int itemPrice) || itemPrice < 0 || string.IsNullOrWhiteSpace(selectedImage))
		{
			GD.PrintErr("Enter a name, a nonnegative numeric price, and a .png, .jpg, or .jpeg image.");
			return;
		}

		try
		{
			await ShopApiClient.AddItemAsync(category, itemName.Text.Trim(), itemPrice, selectedImage);
			GetNode<Window>("AddItemPopup").Hide();
			LoadItems(category);
		}
		catch (Exception error)
		{
			GD.PrintErr("Add avatar item failed: " + error.Message);
		}
	}
}
