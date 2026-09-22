using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class StudentCharacter : Control
{
	private HBoxContainer characterContainer;
	private PanelContainer itemTemplate;
	private Window storylinePopup;
	private HBoxContainer carouselArrows;
	private List<ShopItemDto> characters = new();
	private int selectedIndex;
	private int selectedItemId;
	private Shader blurShader;

	public override void _Ready()
	{
		characterContainer = GetNode<HBoxContainer>("CharacterPanel/MainContainer/CharacterArea/CharacterScroll/CharacterContainer");
		itemTemplate = GetNode<PanelContainer>("StudentCharacterItem");
		storylinePopup = GetNode<Window>("CharacterStorylinePopup");
		carouselArrows = GetNode<HBoxContainer>("CharacterPanel/MainContainer/CarouselArrows");
		GetNode<Button>("CharacterPanel/MainContainer/Header/BackButton").Pressed += OnBackPressed;
		GetNode<Button>("CharacterPanel/MainContainer/CategoryButtons/AvatarButton").Pressed += OnAvatarPressed;
		GetNode<Button>("CharacterPanel/MainContainer/CarouselArrows/LeftArrow").Pressed += MoveLeft;
		GetNode<Button>("CharacterPanel/MainContainer/CarouselArrows/RightArrow").Pressed += MoveRight;
		characterContainer.Alignment = BoxContainer.AlignmentMode.Center;
		blurShader = CreateBlurShader();
		itemTemplate.Visible = false;
		LoadCharacters();
	}

	private async void LoadCharacters()
	{
		try
		{
			characters = await ShopApiClient.GetOwnedAsync("character");
			SelectedCharacterDto selectedCharacter = await ShopApiClient.GetSelectedCharacterAsync();
			selectedItemId = selectedCharacter?.ItemId ?? 0;
			selectedIndex = 0;
			RenderCarousel();
		}
		catch (Exception error)
		{
			GD.PrintErr("Character load failed: " + error.Message);
		}
	}

	private void RenderCarousel()
	{
		ClearCarousel();
		carouselArrows.Visible = characters.Count >= 2;
		if (characters.Count == 0)
			return;

		if (characters.Count == 1)
		{
			AddCharacter(characters[0], true);
			return;
		}

		AddCharacter(GetItem(-1), false);
		AddCharacter(GetItem(0), true);
		if (characters.Count >= 3)
			AddCharacter(GetItem(1), false);
	}

	private void AddCharacter(ShopItemDto item, bool isCenter)
	{
		PanelContainer card = (PanelContainer)itemTemplate.Duplicate();
		card.Visible = true;
		card.Scale = isCenter ? Vector2.One : new Vector2(0.78f, 0.78f);
		card.Modulate = isCenter ? Colors.White : new Color(1, 1, 1, 0.58f);
		card.PivotOffset = new Vector2(115, 195);
		characterContainer.AddChild(card);
		card.GetNode<Label>("ItemContainer/CharacterName").Text = item.ItemName;
		TextureRect image = card.GetNode<TextureRect>("ItemContainer/CharacterImage");
		SetImage(image, item.CharacterItems);
		if (!isCenter)
			image.Material = new ShaderMaterial { Shader = blurShader };
		Button useButton = card.GetNode<Button>("ItemContainer/UseButton");
		useButton.Text = item.ItemId == selectedItemId ? "IN USE" : "USE";
		useButton.Disabled = item.ItemId == selectedItemId;
		useButton.Pressed += async () => await Equip(item.ItemId);
		card.GetNode<Button>("ItemContainer/StorylineButton").Pressed += () => ShowStoryline(item.ItemName);
	}

	private ShopItemDto GetItem(int offset)
	{
		return characters[WrapIndex(selectedIndex + offset, characters.Count)];
	}

	private void MoveLeft()
	{
		if (characters.Count == 0)
			return;

		selectedIndex = WrapIndex(selectedIndex - 1, characters.Count);
		RenderCarousel();
	}

	private void MoveRight()
	{
		if (characters.Count == 0)
			return;

		selectedIndex = WrapIndex(selectedIndex + 1, characters.Count);
		RenderCarousel();
	}

	private static int WrapIndex(int index, int count)
	{
		return (index % count + count) % count;
	}

	private void ClearCarousel()
	{
		foreach (Node child in characterContainer.GetChildren())
			child.QueueFree();
	}

	private async Task Equip(int itemId)
	{
		int previousSelectedItemId = selectedItemId;
		selectedItemId = itemId;
		RenderCarousel();

		try
		{
			await ShopApiClient.EquipCharacterAsync(itemId);
		}
		catch (Exception error)
		{
			selectedItemId = previousSelectedItemId;
			RenderCarousel();
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

	private void OnAvatarPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/Modules/Character/StudentAvatar.tscn");
	}

	private static void SetImage(TextureRect image, string path)
	{
		image.Texture = ShopApiClient.LoadShopImage(path);
	}

	private static Shader CreateBlurShader()
	{
		Shader shader = new Shader();
		shader.Code = "shader_type canvas_item;\n" +
			"void fragment() {\n" +
			"vec2 pixel = TEXTURE_PIXEL_SIZE * 2.0;\n" +
			"vec4 color = texture(TEXTURE, UV) * 0.40;\n" +
			"color += texture(TEXTURE, UV + vec2(pixel.x, 0.0)) * 0.15;\n" +
			"color += texture(TEXTURE, UV - vec2(pixel.x, 0.0)) * 0.15;\n" +
			"color += texture(TEXTURE, UV + vec2(0.0, pixel.y)) * 0.15;\n" +
			"color += texture(TEXTURE, UV - vec2(0.0, pixel.y)) * 0.15;\n" +
			"COLOR = color;\n" +
			"}";
		return shader;
	}
}
