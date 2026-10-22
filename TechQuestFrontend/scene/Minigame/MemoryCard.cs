using Godot;
using System;

public partial class MemoryCard : Button
{
	[Export] public Texture2D CardBackTexture { get; set; }

	private TextureRect cardBack;
	private TextureRect characterImage;
	private Label matchIndicator;
	private Texture2D faceTexture;
	private int cardIndex;
	private bool isMatched;

	public int CardIndex => cardIndex;
	public Texture2D FaceTexture => faceTexture;
	public event Action<MemoryCard> Selected;

	public override void _Ready()
	{
		cardBack = GetNode<TextureRect>("CardContent/CardBack");
		characterImage = GetNode<TextureRect>("CardContent/CharacterImage");
		matchIndicator = GetNode<Label>("CardContent/MatchIndicator");
		FocusMode = FocusModeEnum.None;
		ClipContents = true;
		Pressed += OnPressed;
		if (CardBackTexture != null)
			cardBack.Texture = CardBackTexture;
		ShowBack();
	}

	public void Setup(int index, Texture2D texture)
	{
		cardIndex = index;
		faceTexture = texture;
		if (characterImage != null)
			characterImage.Texture = texture;
		ShowBack();
	}

	public void ShowFace()
	{
		characterImage.Texture = faceTexture;
		characterImage.Show();
		cardBack.Hide();
		matchIndicator.Hide();
	}

	public void ShowBack()
	{
		if (CardBackTexture != null)
			cardBack.Texture = CardBackTexture;
		cardBack.Show();
		characterImage.Hide();
		matchIndicator.Hide();
	}

	public void SetMatched(bool matched)
	{
		isMatched = matched;
		Disabled = matched;
		if (matched)
			matchIndicator.Show();
	}

	public void SetCardDisabled(bool disabled) => Disabled = disabled || isMatched;

	private void OnPressed()
	{
		if (!Disabled && !isMatched)
			Selected?.Invoke(this);
	}
}
