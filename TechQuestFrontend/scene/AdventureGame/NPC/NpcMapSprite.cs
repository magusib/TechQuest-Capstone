using Godot;

public static class NpcMapSprite
{
	public static AnimatedSprite2D CreateIdleSprite(Texture2D texture)
	{
		var frames = new SpriteFrames();
		frames.AddAnimation("Idle");
		frames.AddFrame("Idle", texture);

		var sprite = new AnimatedSprite2D
		{
			SpriteFrames = frames,
			Animation = "Idle",
			Scale = new Vector2(0.5f, 0.5f),
			ZIndex = 5
		};
		sprite.Play("Idle");
		return sprite;
	}
}
