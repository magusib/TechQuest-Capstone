using Godot;

public partial class FallingDuckTarget : Area2D
{
	[Export] public float FallingSpeed { get; set; } = 210;

	public override void _Ready()
	{
		GetNode<Sprite2D>("DuckSprite");
		Monitoring = true;
	}

	public override void _PhysicsProcess(double delta)
	{
		Position = new Vector2(Position.X, Position.Y + FallingSpeed * (float)delta);
		if (Position.Y > GetViewportRect().Size.Y + 80)
			QueueFree();
	}
}
