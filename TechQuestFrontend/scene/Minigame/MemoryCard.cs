using Godot;
using System;

public partial class MemoryCard : Button
{
	[Export]
	public Texture2D CardBackTexture { get; set; }

	private TextureRect image;
	private Texture2D faceTexture;
	private int cardIndex;
	private bool isMatched;

	public int CardIndex => cardIndex;
	public Texture2D FaceTexture => faceTexture;
	public event Action<MemoryCard> Selected;

	public override void _Ready()
	{
		FocusMode = FocusModeEnum.None;
		ClipContents = true;
		image = new TextureRect
		{
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore
		};
		MiniGameUi.Fill(image);
		AddChild(image);
		Pressed += OnPressed;
		ShowBack();
	}

	public void Setup(int index, Texture2D texture)
	{
		cardIndex = index;
		faceTexture = texture;
		ShowBack();
	}

	public void ShowFace()
	{
		image.Texture = faceTexture;
		Text = faceTexture == null ? "?" : "";
	}

	public void ShowBack()
	{
		if (image != null)
			image.Texture = CardBackTexture;
		Text = CardBackTexture == null ? "?" : "";
	}

	public void SetMatched(bool matched)
	{
		isMatched = matched;
		Disabled = matched;
	}

	public void SetCardDisabled(bool disabled)
	{
		Disabled = disabled || isMatched;
	}

	private void OnPressed()
	{
		if (!Disabled && !isMatched)
			Selected?.Invoke(this);
	}
}
