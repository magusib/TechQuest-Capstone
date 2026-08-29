using Godot;

public partial class Player : CharacterBody2D
{
	private const float SPEED = 120.0f;
	private const float JUMP_VELOCITY = -300.0f;

	private float gravity;
	private AnimatedSprite2D animatedSprite;

	public override void _Ready()
	{
		gravity = ProjectSettings
			.GetSetting("physics/2d/default_gravity")
			.AsSingle();

		animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		// Default animation
		animatedSprite.Play("Idle");
	}

	public override void _PhysicsProcess(double delta)
	{
		// Gravity
		if (!IsOnFloor())
		{
			Velocity = new Vector2(
				Velocity.X,
				Velocity.Y + gravity * (float)delta
			);
		}

		// Jump
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			Velocity = new Vector2(
				Velocity.X,
				JUMP_VELOCITY
			);
		}

		// Get movement direction
		float direction = Input.GetAxis("move_left", "move_right");

		// Flip character
		if (direction > 0)
		{
			animatedSprite.FlipH = false;
		}
		else if (direction < 0)
		{
			animatedSprite.FlipH = true;
		}

		// WALK
		if (direction != 0)
		{
			Velocity = new Vector2(
				direction * SPEED,
				Velocity.Y
			);

			animatedSprite.Play("Walk");
		}
		// IDLE
		else
		{
			Velocity = new Vector2(
				Mathf.MoveToward(Velocity.X, 0, SPEED),
				Velocity.Y
			);

			animatedSprite.Play("Idle");
		}

		MoveAndSlide();
	}
}
