using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class AdminShop : Control
{
	private Button backButton;
	private GridContainer itemGrid;
	private Panel itemTemplate;
	private Window addItemPopup;
	private LineEdit itemName;
	private LineEdit price;
	private Button imageButton;
	private string selectedImage = "";
	private string category = "character";

	public override void _Ready()
	{
		backButton = GetNode<Button>("BackButton");
		backButton.Pressed += OnBackPressed;
		itemGrid = GetNode<GridContainer>("ShopPanel/ScrollContainer/ItemGrid");
		itemTemplate = GetNode<Panel>("ShopPanel/ItemPanel");
		addItemPopup = GetNode<Window>("AddItemPopup");
		itemName = GetNode<LineEdit>("AddItemPopup/MarginContainer/VBoxContainer/ItemName");
		price = GetNode<LineEdit>("AddItemPopup/MarginContainer/VBoxContainer/Price");
		imageButton = GetNode<Button>("AddItemPopup/MarginContainer/VBoxContainer/ImageButton");

		GetNode<Button>("ShopPanel/CategoryButtons/CharacterButton").Pressed += () => LoadItems("character");
		GetNode<Button>("ShopPanel/CategoryButtons/AvatarButton").Pressed += () => LoadItems("avatar");
		GetNode<Button>("ShopPanel/AddItemButton").Pressed += OpenAddItem;
		GetNode<Button>("AddItemPopup/MarginContainer/VBoxContainer/Buttons/CancelButton").Pressed += () => addItemPopup.Hide();
		GetNode<Button>("AddItemPopup/MarginContainer/VBoxContainer/Buttons/AddButton").Pressed += AddItem;
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
			itemGrid.Columns = Math.Max(1, items.Count);
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
		itemGrid.AddChild(card);
		card.GetNode<Label>("Label").Text = item.PriceCoins + " Coins";
		card.GetNode<Label>("Label2").Text = item.ItemName;
		SetImage(card.GetNode<TextureRect>("ItemPlaceholder"), item.CharacterItems ?? item.AvatarItems);
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
		addItemPopup.PopupCentered();
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
			addItemPopup.Hide();
			LoadItems(category);
		}
		catch (Exception error)
		{
			GD.PrintErr("Add shop item failed: " + error.Message);
		}
	}

	private static void SetImage(TextureRect image, string path)
	{
		if (!string.IsNullOrWhiteSpace(path) && ResourceLoader.Exists(path))
			image.Texture = GD.Load<Texture2D>(path);
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/UserDashboard/Admin Dashboard/AdminDashboard.tscn");
	}
}
