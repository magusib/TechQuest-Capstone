using Godot;

public partial class TapTarget : Button
{
	[Export]
	public Texture2D TargetTexture { get; set; }

	[Export]
	public float TargetSize { get; set; } = 140;

	public void Configure(Texture2D texture, float size)
	{
		TargetTexture = texture;
		TargetSize = size;
		CustomMinimumSize = new Vector2(size, size);
		Size = new Vector2(size, size);
	}

	public override void _Ready()
	{
		FocusMode = FocusModeEnum.None;
		MiniGameUi.ApplyButtonStyle(this);
		CustomMinimumSize = new Vector2(TargetSize, TargetSize);
		Size = new Vector2(TargetSize, TargetSize);
		if (TargetTexture == null)
		{
			Text = "TAP!";
			return;
		}

		TextureRect image = new()
		{
			Texture = TargetTexture,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore
		};
		MiniGameUi.Fill(image);
		AddChild(image);
		Text = "";
	}
}
