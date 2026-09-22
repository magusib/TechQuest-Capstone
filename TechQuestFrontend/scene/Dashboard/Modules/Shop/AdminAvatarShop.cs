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
	private Button popupActionButton;
	private Label popupTitle;
	private ConfirmationDialog deleteConfirmation;
	private string existingImagePath = "";
	private ShopItemDto editingItem;
	private ShopItemDto deletingItem;

	public override void _Ready()
	{
		itemGrid = GetNode<GridContainer>("ShopPanel/ScrollContainer/ItemGrid");
		itemTemplate = GetNode<Panel>("ItemPanel");
		itemName = GetNode<LineEdit>("AddItemPopup/MarginContainer/VBoxContainer/ItemName");
		price = GetNode<LineEdit>("AddItemPopup/MarginContainer/VBoxContainer/Price");
		imageButton = GetNode<Button>("AddItemPopup/MarginContainer/VBoxContainer/ImageButton");
		popupTitle = GetNode<Label>("AddItemPopup/MarginContainer/VBoxContainer/Title");
		popupActionButton = GetNode<Button>("AddItemPopup/MarginContainer/VBoxContainer/Buttons/AddButton");
		deleteConfirmation = GetNode<ConfirmationDialog>("DeleteItemConfirmation");
		GetNode<Button>("ShopPanel/AddItemButton").Pressed += OpenAddItem;
		GetNode<Button>("AddItemPopup/MarginContainer/VBoxContainer/Buttons/CancelButton").Pressed += () => GetNode<Window>("AddItemPopup").Hide();
		popupActionButton.Pressed += SaveItem;
		deleteConfirmation.Confirmed += ConfirmDelete;
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
				card.GetNode<TextureRect>("ItemPlaceholder").Texture = ShopApiClient.LoadShopImage(imagePath);
				card.GetNode<Button>("ActionContainer/EditButton").Pressed += () => OpenEditItem(item);
				card.GetNode<Button>("ActionContainer/DeleteButton").Pressed += () => AskDelete(item);
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
		editingItem = null;
		existingImagePath = "";
		itemName.Text = "";
		price.Text = "0";
		selectedImage = "";
		popupTitle.Text = "ADD SHOP ITEM";
		popupActionButton.Text = "ADD ITEM";
		imageButton.Text = "SELECT IMAGE (.png / .jpg / .jpeg)";
		GetNode<Window>("AddItemPopup").PopupCentered();
	}

	private void OpenEditItem(ShopItemDto item)
	{
		editingItem = item;
		existingImagePath = category == "avatar" ? item.AvatarItems : item.CharacterItems;
		itemName.Text = item.ItemName;
		price.Text = item.PriceCoins.ToString();
		selectedImage = "";
		popupTitle.Text = "EDIT SHOP ITEM";
		popupActionButton.Text = "SAVE";
		imageButton.Text = string.IsNullOrWhiteSpace(existingImagePath)
			? "SELECT IMAGE (.png / .jpg / .jpeg)"
			: existingImagePath.GetFile();
		GetNode<Window>("AddItemPopup").PopupCentered();
	}

	private void SelectImage()
	{
		FileDialog dialog = new FileDialog();
		dialog.Access = FileDialog.AccessEnum.Filesystem;
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

	private async void SaveItem()
	{
		if (string.IsNullOrWhiteSpace(itemName.Text) || !int.TryParse(price.Text, out int itemPrice) || itemPrice < 0
			|| (editingItem == null && string.IsNullOrWhiteSpace(selectedImage)))
		{
			GD.PrintErr("Enter a name, a nonnegative numeric price, and a .png, .jpg, or .jpeg image.");
			return;
		}

		string copiedImagePath = "";
		try
		{
			string imagePath = existingImagePath;
			if (!string.IsNullOrWhiteSpace(selectedImage))
			{
				copiedImagePath = ShopApiClient.CopyShopImageToProject(category, selectedImage);
				imagePath = copiedImagePath;
			}

			if (editingItem == null)
				await ShopApiClient.AddItemAsync(category, itemName.Text.Trim(), itemPrice, imagePath);
			else
			{
				await ShopApiClient.UpdateItemAsync(editingItem.ItemId, category, itemName.Text.Trim(), itemPrice, imagePath);
				if (!string.IsNullOrWhiteSpace(copiedImagePath))
					ShopApiClient.DeleteCopiedShopImage(existingImagePath);
			}

			GetNode<Window>("AddItemPopup").Hide();
			LoadItems(category);
		}
		catch (Exception error)
		{
			ShopApiClient.DeleteCopiedShopImage(copiedImagePath);
			GD.PrintErr("Add avatar item failed: " + error.Message);
			ShowMessage(error.Message);
		}
	}

	private void AskDelete(ShopItemDto item)
	{
		deletingItem = item;
		ConfirmationDialog dialog = deleteConfirmation;
		dialog.DialogText = "Delete '" + item.ItemName + "' from the shop?";
		dialog.PopupCentered();
	}

	private async void ConfirmDelete()
	{
		if (deletingItem == null)
			return;

		ShopItemDto item = deletingItem;
		deletingItem = null;
		try
		{
			await ShopApiClient.DeleteItemAsync(item.ItemId);
			string imagePath = category == "avatar" ? item.AvatarItems : item.CharacterItems;
			ShopApiClient.DeleteCopiedShopImage(imagePath);
			LoadItems(category);
		}
		catch (Exception error)
		{
			GD.PrintErr("Delete avatar shop item failed: " + error.Message);
			ShowMessage(error.Message);
		}
	}

	private void ShowMessage(string message)
	{
		AcceptDialog dialog = new AcceptDialog { DialogText = message };
		AddChild(dialog);
		dialog.PopupCentered();
	}
}
