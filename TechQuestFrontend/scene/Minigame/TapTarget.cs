using Godot;

public partial class TapTarget : Button
{
	[Export] public Texture2D TargetTexture { get; set; }
	[Export] public float TargetSize { get; set; } = 140;

	public override void _Ready()
	{
		FocusMode = FocusModeEnum.None;
		CustomMinimumSize = new Vector2(TargetSize, TargetSize);
		Size = new Vector2(TargetSize, TargetSize);
		if (TargetTexture != null)
			GetNode<TextureRect>("CharacterImage").Texture = TargetTexture;
	}
}
