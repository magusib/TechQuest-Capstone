
using Godot;

public partial class Character : CharacterBody2D
{
	private const float SPEED = 300.0f;
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
		float deltaFloat = (float)delta;

		// =========================
		// GRAVITY
		// =========================
		if (!IsOnFloor())
		{
			Velocity = new Vector2(
				Velocity.X,
				Velocity.Y + gravity * deltaFloat
			);
		}

		// =========================
		// MOVEMENT
		// =========================
		float direction = Input.GetAxis("move_left", "move_right");

		if (direction != 0)
		{
			Velocity = new Vector2(
				direction * SPEED,
				Velocity.Y
			);

			// Flip character
			if (direction > 0)
			{
				animatedSprite.FlipH = false;
			}
			else if (direction < 0)
			{
				animatedSprite.FlipH = true;
			}
		}
		else
		{
			Velocity = new Vector2(
				Mathf.MoveToward(Velocity.X, 0, SPEED),
				Velocity.Y
			);
		}

		// =========================
		// JUMP
		// =========================
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			Velocity = new Vector2(
				Velocity.X,
				JUMP_VELOCITY
			);

			// Play jump animation
			animatedSprite.Play("Jump");
		}

		// =========================
		// ANIMATION
		// =========================

		// If player is in the air,
		// keep the Jump animation.
		if (!IsOnFloor())
		{
			animatedSprite.Play("Jump");
		}
		else
		{
			// Player is on the ground

			if (direction != 0)
			{
				animatedSprite.Play("Walk");
			}
			else
			{
				animatedSprite.Play("Idle");
			}
		}

		// =========================
		// APPLY MOVEMENT
		// =========================
		MoveAndSlide();
	}
}
