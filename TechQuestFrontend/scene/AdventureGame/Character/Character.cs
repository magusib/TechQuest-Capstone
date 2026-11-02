
using Godot;
using System;

public partial class Character : CharacterBody2D
{
	private const float SPEED = 200.0f;
	private const float JUMP_VELOCITY = -450.0f;

	private float gravity;
	private AnimatedSprite2D animatedSprite;
	private Label nameLabel;
	private double walkingRecoverySeconds;
	private bool walkingRecoveryInProgress;
	private Vector2 previousPosition;

	public override void _Ready()
	{
		gravity = ProjectSettings
			.GetSetting("physics/2d/default_gravity")
			.AsSingle();

		animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		nameLabel = new Label
		{
			Text = UserSession.FullName,
			Position = new Vector2(-60, -50),
			Size = new Vector2(120, 18),
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = 1
		};
		nameLabel.AddThemeFontSizeOverride("font_size", 8);
		AddChild(nameLabel);

		// Default animation
		animatedSprite.Play("Idle");
		previousPosition = GlobalPosition;
		GD.Print($"Lifespan walking recovery enabled. IsStudent={UserSession.IsStudent}, userId={UserSession.UserId}");
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

		// Measure actual displacement after MoveAndSlide. This is more reliable than
		// Velocity.X because collision and input-release timing can make velocity 0
		// even while the character is visibly moving for a short period.
		float distanceMoved = GlobalPosition.DistanceTo(previousPosition);
		previousPosition = GlobalPosition;
		bool isActivelyWalking = distanceMoved > 0.05f && IsOnFloor();
		if (UserSession.IsStudent && isActivelyWalking && !walkingRecoveryInProgress)
		{
			walkingRecoverySeconds += delta;
			if (walkingRecoverySeconds >= 60)
			{
				walkingRecoverySeconds = 0;
				GD.Print($"One minute of active walking completed. Requesting +1 Lifespan. Current={UserSession.Lifespan}/{UserSession.MaximumLifespan}");
				RecoverFromWalkingAsync();
			}
		}
		else if (!isActivelyWalking)
		{
			walkingRecoverySeconds = 0;
		}
	}

	private async void RecoverFromWalkingAsync()
	{
		walkingRecoveryInProgress = true;
		try
		{
			LifespanResponseDto response = await LifespanApiClient.WalkingRecoveryAsync();
			UserSession.UpdateLifespan(response.Lifespan);
			UserSession.UpdateStudentProfile(response.TotalCoins, UserSession.Points);
			GD.Print($"Walking recovery succeeded. Lifespan is now {response.Lifespan}/{UserSession.MaximumLifespan}.");
		}
		catch (Exception exception)
		{
			GD.PrintErr("Walking recovery failed: " + exception.Message);
		}
		finally
		{
			walkingRecoveryInProgress = false;
		}
	}
}
