using Godot;
using System;

public partial class FallingDuckTarget : Area2D
{
	[Export]
	public Texture2D DuckTexture { get; set; }

	[Export]
	public float FallingSpeed { get; set; } = 210;

	[Export]
	public Vector2 DuckSize { get; set; } = new Vector2(70, 54);

	private Sprite2D sprite;
	private CollisionShape2D collision;

	public override void _Ready()
	{
		Monitoring = true;
		sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (sprite == null || collision == null)
			return;

		sprite.Texture = DuckTexture;
		if (DuckTexture != null)
		{
			sprite.Scale = new Vector2(
				DuckSize.X / Math.Max(1, DuckTexture.GetWidth()),
				DuckSize.Y / Math.Max(1, DuckTexture.GetHeight()));
			sprite.Modulate = Colors.White;
		}
		else
		{
			Polygon2D placeholder = new()
			{
				Polygon = new Vector2[]
				{
					new Vector2(-32, -8),
					new Vector2(-22, -22),
					new Vector2(5, -22),
					new Vector2(24, -8),
					new Vector2(35, -5),
					new Vector2(22, 4),
					new Vector2(8, 20),
					new Vector2(-18, 19),
					new Vector2(-34, 8)
				},
				Color = new Color(1, 0.85f, 0.25f)
			};
			AddChild(placeholder);
		}

		CircleShape2D circle = new()
		{
			Radius = Math.Max(DuckSize.X, DuckSize.Y) / 2.5f
		};
		collision.Shape = circle;
	}

	public void Configure(Texture2D texture, float speed, Vector2 size)
	{
		DuckTexture = texture;
		FallingSpeed = Math.Max(1, speed);
		DuckSize = size;
	}

	public override void _Process(double delta)
	{
		Position = new Vector2(Position.X, Position.Y + FallingSpeed * (float)delta);
		if (Position.Y > GetViewportRect().Size.Y + DuckSize.Y)
			QueueFree();
	}
}
