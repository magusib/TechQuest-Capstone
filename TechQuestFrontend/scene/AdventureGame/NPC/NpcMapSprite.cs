
using Godot;
using System;

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
			Offset = new Vector2(0, -texture.GetHeight() / 2.0f),
			ZIndex = 5
		};

		sprite.SetMeta("GroundAnchorOffset", Vector2.Zero);
		AddNpcInteractionArea(sprite);
		sprite.Play("Idle");

		return sprite;
	}

	public static AnimatedSprite2D CreateNpcSprite(
		Texture2D texture,
		string animationScenePath)
	{
		if (string.IsNullOrWhiteSpace(animationScenePath))
		{
			if (texture == null)
				throw new InvalidOperationException(
					"NPC image could not be loaded.");

			return CreateIdleSprite(texture);
		}

		PackedScene packedScene = GD.Load<PackedScene>(
			animationScenePath);

		if (packedScene == null)
			throw new InvalidOperationException(
				$"NPC animation scene '{animationScenePath}' could not be loaded.");

		Node sceneInstance = packedScene.Instantiate();

		try
		{
			AnimatedSprite2D sourceSprite =
				FindAnimatedSprite(sceneInstance);

			if (sourceSprite?.SpriteFrames == null)
				throw new InvalidOperationException(
					$"NPC animation scene '{animationScenePath}' has no AnimatedSprite2D frames.");

			CollisionShape2D collisionShape =
				FindCollisionShape(sceneInstance);

			if (collisionShape?.Shape == null)
				throw new InvalidOperationException(
					$"NPC animation scene '{animationScenePath}' has no CollisionShape2D shape.");

			SpriteFrames spriteFrames = sourceSprite.SpriteFrames;
			StringName animation = sourceSprite.Animation;

			if (!spriteFrames.HasAnimation(animation) ||
				spriteFrames.GetFrameCount(animation) == 0)
			{
				var animations = spriteFrames.GetAnimationNames();

				if (animations.Length == 0 ||
					spriteFrames.GetFrameCount(animations[0]) == 0)
				{
					throw new InvalidOperationException(
						$"NPC animation scene '{animationScenePath}' has no frames.");
				}

				animation = animations[0];
			}

			Texture2D firstFrame =
				spriteFrames.GetFrameTexture(animation, 0);

			if (firstFrame == null)
				throw new InvalidOperationException(
					$"First animation frame in '{animationScenePath}' has no texture.");

			float scaleY = sourceSprite.Scale.Y;

			if (Mathf.IsZeroApprox(scaleY))
				throw new InvalidOperationException(
					$"AnimatedSprite2D in '{animationScenePath}' has a zero vertical scale.");

			float collisionBottom = collisionShape.Position.Y +
				collisionShape.Scale.Y *
				collisionShape.Shape.GetRect().End.Y;

			float frameBottom = sourceSprite.Centered
				? firstFrame.GetHeight() / 2.0f
				: firstFrame.GetHeight();

			float spriteOffsetY =
				(collisionBottom - sourceSprite.Position.Y) / scaleY
				- frameBottom;

			var animatedSprite = new AnimatedSprite2D
			{
				SpriteFrames = spriteFrames,
				Animation = animation,
				Scale = sourceSprite.Scale,
				Centered = sourceSprite.Centered,
				Offset = new Vector2(
					sourceSprite.Offset.X,
					spriteOffsetY),
				ZIndex = sourceSprite.ZIndex,
				TextureFilter = sourceSprite.TextureFilter
			};

			animatedSprite.SetMeta(
				"GroundAnchorOffset",
				new Vector2(0, collisionBottom));

			AddNpcInteractionArea(animatedSprite);
			animatedSprite.Play(animation);

			return animatedSprite;
		}
		finally
		{
			sceneInstance.Free();
		}
	}

	private static void AddNpcInteractionArea(AnimatedSprite2D sprite)
	{
		NpcInteractionArea interactionArea = new()
		{
			Name = "NpcInteractionArea",
			Position = new Vector2(0, -40)
		};
		CollisionShape2D collision = new()
		{
			Shape = new CircleShape2D { Radius = 48 }
		};
		interactionArea.AddChild(collision);
		sprite.AddChild(interactionArea);
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

	private static CollisionShape2D FindCollisionShape(Node node)
	{
		if (node is CollisionShape2D collisionShape)
			return collisionShape;

		foreach (Node child in node.GetChildren())
		{
			CollisionShape2D shape = FindCollisionShape(child);

			if (shape != null)
				return shape;
		}

		return null;
	}

	public static void PlaceAtGround(
		AnimatedSprite2D sprite,
		Vector2 groundPosition)
	{
		Vector2 anchorOffset = sprite.HasMeta("GroundAnchorOffset")
			? sprite.GetMeta("GroundAnchorOffset").AsVector2()
			: Vector2.Zero;

		// Adjust this value to move the NPC down.
		const float dropOffset = 20.0f;

		sprite.GlobalPosition =
			groundPosition
			- anchorOffset
			+ new Vector2(0, dropOffset);
	}

	public static bool HasNpcSprite(Node currentScene, int npcId)
	{
		if (currentScene == null)
			return false;

		foreach (Node child in currentScene.GetChildren())
		{
			if (child is AnimatedSprite2D sprite &&
				sprite.HasMeta("NpcId") &&
				sprite.GetMeta("NpcId").AsInt32() == npcId)
			{
				return true;
			}
		}

		return false;
	}

	public static bool HasDeploymentSprite(
		Node currentScene,
		int deploymentId)
	{
		if (currentScene == null)
			return false;

		foreach (Node child in currentScene.GetChildren())
		{
			if (child is AnimatedSprite2D sprite &&
				sprite.HasMeta("DeploymentId") &&
				sprite.GetMeta("DeploymentId").AsInt32() == deploymentId)
			{
				return true;
			}
		}

		return false;
	}

	public static void RemoveDeploymentSprite(Node currentScene, int deploymentId)
	{
		if (currentScene == null)
			return;

		foreach (Node child in currentScene.GetChildren())
		{
			if (child is AnimatedSprite2D sprite &&
				sprite.HasMeta("DeploymentId") &&
				sprite.GetMeta("DeploymentId").AsInt32() == deploymentId)
			{
				sprite.QueueFree();
			}
		}
	}
}
