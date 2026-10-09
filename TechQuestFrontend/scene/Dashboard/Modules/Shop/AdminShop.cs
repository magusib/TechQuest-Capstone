using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class AdminShop : Control
{
	private Button backButton;
	private GridContainer itemGrid;
	private Panel itemTemplate;
	private Panel addItemPopup;
	private LineEdit itemName;
	private LineEdit price;
	private Button imageButton;
	private Button popupActionButton;
	private Label popupTitle;
	private Panel storylinePopup;
	private Label storylineCharacter;
	private TextEdit storylineInput;
	private ShopItemDto storylineItem;
	private ConfirmationDialog deleteConfirmation;
	private string selectedImage = "";
	private string existingImagePath = "";
	private string category = "character";
	private ShopItemDto editingItem;
	private ShopItemDto deletingItem;

	public override void _Ready()
	{
		backButton = GetNode<Button>("BackButton");
		backButton.Pressed += OnBackPressed;
		itemGrid = GetNode<GridContainer>("ShopPanel/ScrollContainer/ItemGrid");
		itemTemplate = GetNode<Panel>("ShopPanel/ItemPanel");
		addItemPopup = GetNode<Panel>("AddItemPopup");
		itemName = GetNode<LineEdit>("AddItemPopup/MarginContainer/VBoxContainer/ItemName");
		price = GetNode<LineEdit>("AddItemPopup/MarginContainer/VBoxContainer/Price");
		imageButton = GetNode<Button>("AddItemPopup/MarginContainer/VBoxContainer/ImageButton");
		popupTitle = GetNode<Label>("AddItemPopup/MarginContainer/VBoxContainer/Title");
		popupActionButton = GetNode<Button>("AddItemPopup/MarginContainer/VBoxContainer/Buttons/AddButton");
		storylinePopup = GetNode<Panel>("StorylinePopup");
		storylineCharacter = GetNode<Label>("StorylinePopup/MarginContainer/VBoxContainer/CharacterName");
		storylineInput = GetNode<TextEdit>("StorylinePopup/MarginContainer/VBoxContainer/StorylineInput");
		deleteConfirmation = GetNode<ConfirmationDialog>("DeleteItemConfirmation");

		GetNode<Button>("ShopPanel/CategoryButtons/CharacterButton").Pressed += () => LoadItems("character");
		GetNode<Button>("ShopPanel/CategoryButtons/AvatarButton").Pressed += () => LoadItems("avatar");
		GetNode<Button>("ShopPanel/AddItemButton").Pressed += OpenAddItem;
		GetNode<Button>("AddItemPopup/MarginContainer/VBoxContainer/Buttons/CancelButton").Pressed += () => addItemPopup.Hide();
		popupActionButton.Pressed += SaveItem;
		GetNode<Button>("StorylinePopup/MarginContainer/VBoxContainer/Buttons/CancelButton").Pressed += storylinePopup.Hide;
		GetNode<Button>("StorylinePopup/MarginContainer/VBoxContainer/Buttons/SaveButton").Pressed += SaveStoryline;
		deleteConfirmation.Confirmed += ConfirmDelete;
		imageButton.Pressed += SelectImage;
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
				AddItemCard(item);
		}
		catch (Exception error)
		{
			GD.PrintErr("Admin shop load failed: " + error.Message);
		}
	}

	private void AddItemCard(ShopItemDto item)
	{
		Panel card = (Panel)itemTemplate.Duplicate();
		card.Visible = true;
		card.CustomMinimumSize = itemTemplate.Size;
		itemGrid.AddChild(card);
		card.GetNode<Label>("Label").Text = item.PriceCoins + " Coins";
		card.GetNode<Label>("Label2").Text = item.ItemName;
		SetImage(card.GetNode<TextureRect>("ItemPlaceholder"), category == "avatar" ? item.AvatarItems : item.CharacterItems);
		card.GetNode<Button>("ActionContainer/EditButton").Pressed += () => OpenEditItem(item);
		card.GetNode<Button>("ActionContainer/DeleteButton").Pressed += () => AskDelete(item);
		Button storylineButton = card.GetNode<Button>("StorylineButton");
		storylineButton.Visible = category == "character";
		if (category == "character")
		{
			storylineButton.Text = string.IsNullOrWhiteSpace(item.Storyline) ? "ADD STORYLINE" : "EDIT STORYLINE";
			storylineButton.Pressed += () => OpenStoryline(item);
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
		addItemPopup.Show();
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
		addItemPopup.Show();
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

	private void OpenStoryline(ShopItemDto item)
	{
		storylineItem = item;
		storylineCharacter.Text = "Character: " + item.ItemName;
		storylineInput.Text = item.Storyline ?? "";
		storylinePopup.Show();
	}

	private async void SaveStoryline()
	{
		if (storylineItem == null || string.IsNullOrWhiteSpace(storylineInput.Text))
		{
			ShowMessage("Storyline is required.");
			return;
		}

		try
		{
			await ShopApiClient.UpdateStorylineAsync(storylineItem.ItemId, storylineInput.Text.Trim());
			storylinePopup.Hide();
			LoadItems(category);
		}
		catch (Exception error)
		{
			GD.PrintErr("Storyline save failed: " + error.Message);
			ShowMessage(error.Message);
		}
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

			addItemPopup.Hide();
			LoadItems(category);
		}
		catch (Exception error)
		{
			ShopApiClient.DeleteCopiedShopImage(copiedImagePath);
			GD.PrintErr("Add shop item failed: " + error.Message);
			ShowMessage(error.Message);
		}
	}

	private void AskDelete(ShopItemDto item)
	{
		deletingItem = item;
		deleteConfirmation.DialogText = "Delete '" + item.ItemName + "' from the shop?";
		deleteConfirmation.PopupCentered();
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
			GD.PrintErr("Delete shop item failed: " + error.Message);
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
		GetTree().ChangeSceneToFile("res://scene/Dashboard/UserDashboard/Admin Dashboard/AdminDashboard.tscn");
	}
}
