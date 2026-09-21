using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class StudentAvatar : Control
{
	private HBoxContainer avatarContainer;
	private PanelContainer itemTemplate;
	private HBoxContainer carouselArrows;
	private List<ShopItemDto> avatars = new();
	private int selectedIndex;
	private Shader blurShader;

	public override void _Ready()
	{
		avatarContainer = GetNode<HBoxContainer>("AvatarPanel/MainContainer/AvatarArea/AvatarScroll/AvatarContainer");
		itemTemplate = GetNode<PanelContainer>("StudentAvatarItem");
		carouselArrows = GetNode<HBoxContainer>("AvatarPanel/MainContainer/CarouselArrows");
		GetNode<Button>("AvatarPanel/MainContainer/Header/BackButton").Pressed += OnClosePressed;
		GetNode<Button>("AvatarPanel/MainContainer/CategoryButtons/CharacterButton").Pressed += OnCharacterPressed;
		GetNode<Button>("AvatarPanel/MainContainer/CarouselArrows/LeftArrow").Pressed += MoveLeft;
		GetNode<Button>("AvatarPanel/MainContainer/CarouselArrows/RightArrow").Pressed += MoveRight;
		avatarContainer.Alignment = BoxContainer.AlignmentMode.Center;
		blurShader = CreateBlurShader();
		itemTemplate.Visible = false;
		LoadAvatars();
	}

	private async void LoadAvatars()
	{
		try
		{
			avatars = await ShopApiClient.GetOwnedAsync("avatar");
			selectedIndex = 0;
			RenderCarousel();
		}
		catch (Exception error)
		{
			GD.PrintErr("Avatar load failed: " + error.Message);
		}
	}

	private void RenderCarousel()
	{
		ClearCarousel();
		carouselArrows.Visible = avatars.Count >= 2;
		if (avatars.Count == 0)
			return;

		if (avatars.Count == 1)
		{
			AddAvatar(avatars[0], true);
			return;
		}

		AddAvatar(GetItem(-1), false);
		AddAvatar(GetItem(0), true);
		if (avatars.Count >= 3)
			AddAvatar(GetItem(1), false);
	}

	private void AddAvatar(ShopItemDto item, bool isCenter)
	{
		PanelContainer card = (PanelContainer)itemTemplate.Duplicate();
		card.Visible = true;
		card.Scale = isCenter ? Vector2.One : new Vector2(0.78f, 0.78f);
		card.Modulate = isCenter ? Colors.White : new Color(1, 1, 1, 0.58f);
		card.PivotOffset = new Vector2(115, 195);
		avatarContainer.AddChild(card);
		card.GetNode<Label>("ItemContainer/AvatarName").Text = item.ItemName;
		TextureRect image = card.GetNode<TextureRect>("ItemContainer/AvatarImage");
		image.Texture = ShopApiClient.LoadShopImage(item.AvatarItems);
		if (!isCenter)
			image.Material = new ShaderMaterial { Shader = blurShader };
		card.GetNode<Button>("ItemContainer/UseButton").Pressed += async () => await Equip(item.ItemId);
	}

	private ShopItemDto GetItem(int offset)
	{
		return avatars[WrapIndex(selectedIndex + offset, avatars.Count)];
	}

	private void MoveLeft()
	{
		if (avatars.Count == 0)
			return;

		selectedIndex = WrapIndex(selectedIndex - 1, avatars.Count);
		RenderCarousel();
	}

	private void MoveRight()
	{
		if (avatars.Count == 0)
			return;

		selectedIndex = WrapIndex(selectedIndex + 1, avatars.Count);
		RenderCarousel();
	}

	private static int WrapIndex(int index, int count)
	{
		return (index % count + count) % count;
	}

	private void ClearCarousel()
	{
		foreach (Node child in avatarContainer.GetChildren())
			child.QueueFree();
	}

	private async Task Equip(int itemId)
	{
		try
		{
			await ShopApiClient.EquipAvatarAsync(itemId);
		}
		catch (Exception error)
		{
			GD.PrintErr("Avatar equip failed: " + error.Message);
		}
	}

	private void OnClosePressed()
	{
		QueueFree();
	}

	private void OnCharacterPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/Modules/Character/StudentCharacter.tscn");
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
