using Godot;

public static class MiniGameUi
{
	public static Label CreateLabel(string text, int fontSize, Color color)
	{
		Label label = new()
		{
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	public static Button CreateButton(string text, Vector2 minimumSize)
	{
		Button button = new()
		{
			Text = text,
			CustomMinimumSize = minimumSize,
			FocusMode = Control.FocusModeEnum.None
		};
		ApplyButtonStyle(button);
		return button;
	}

	public static void ApplyButtonStyle(Button button)
	{
		button.AddThemeFontSizeOverride("font_size", 20);
		button.AddThemeColorOverride("font_color", new Color(0.12f, 0.18f, 0.25f));

		StyleBoxFlat normal = new()
		{
			BgColor = new Color(0.98f, 0.83f, 0.39f),
			CornerRadiusTopLeft = 12,
			CornerRadiusTopRight = 12,
			CornerRadiusBottomLeft = 12,
			CornerRadiusBottomRight = 12,
			ContentMarginLeft = 12,
			ContentMarginRight = 12,
			ContentMarginTop = 8,
			ContentMarginBottom = 8
		};
		button.AddThemeStyleboxOverride("normal", normal);

		StyleBoxFlat pressed = new()
		{
			BgColor = new Color(0.91f, 0.69f, 0.24f),
			CornerRadiusTopLeft = 12,
			CornerRadiusTopRight = 12,
			CornerRadiusBottomLeft = 12,
			CornerRadiusBottomRight = 12,
			ContentMarginLeft = 12,
			ContentMarginRight = 12,
			ContentMarginTop = 8,
			ContentMarginBottom = 8
		};
		button.AddThemeStyleboxOverride("pressed", pressed);
	}

	public static void Fill(Control control)
	{
		control.AnchorLeft = 0;
		control.AnchorTop = 0;
		control.AnchorRight = 1;
		control.AnchorBottom = 1;
	}
}
