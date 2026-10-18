using Godot;

public partial class NpcControl : StaticBody2D
{
	public override void _Ready()
	{
		AnimatedSprite2D animatedSprite = FindAnimatedSprite(this);
		if (animatedSprite?.SpriteFrames == null)
			return;

		if (animatedSprite.SpriteFrames.HasAnimation(animatedSprite.Animation) &&
			animatedSprite.SpriteFrames.GetFrameCount(animatedSprite.Animation) > 0)
			animatedSprite.Play(animatedSprite.Animation);
	}

	private static AnimatedSprite2D FindAnimatedSprite(Node node)
	{
		if (node is AnimatedSprite2D animatedSprite)
			return animatedSprite;

		foreach (Node child in node.GetChildren())
		{
			AnimatedSprite2D sprite = FindAnimatedSprite(child);
			if (sprite != null)
				return sprite;
		}

		return null;
	}
}