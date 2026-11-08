
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
		bool textInputFocused = IsTextInputFocused();
		float direction = textInputFocused
			? 0
			: Input.GetAxis("move_left", "move_right");

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
		if (!textInputFocused && Input.IsActionJustPressed("jump") && IsOnFloor())
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

		// Count active left/right input, not the direction of travel. This means
		// walking left, then right, then left again continues the same minute timer.
		// A brief direction change will not reset the accumulated walking time.
		// Use the horizontal input itself as the walking signal. IsOnFloor() can
		// briefly be false on slopes, tile seams, or during a physics update even
		// though the player is still walking, which would reset the one-minute timer.
		bool isActivelyWalking = !Mathf.IsZeroApprox(direction);
		if (UserSession.IsStudent && isActivelyWalking && !walkingRecoveryInProgress)
		{
			walkingRecoverySeconds += delta;
			UserSession.UpdateWalkingRecoveryProgress(walkingRecoverySeconds);
			if (walkingRecoverySeconds >= 60)
			{
				walkingRecoverySeconds = 0;
				GD.Print($"One minute of active walking completed. Requesting +1 Lifespan. Current={UserSession.Lifespan}/{UserSession.MaximumLifespan}");
				RecoverFromWalkingAsync();
			}
		}
		// Do not reset the timer when the player stops, turns around, or jumps.
		// The timer pauses and resumes when horizontal movement starts again.
	}

	private bool IsTextInputFocused()
	{
		Control focusedControl = GetViewport().GuiGetFocusOwner() as Control;
		return focusedControl is LineEdit || focusedControl is TextEdit;
	}

	private async void RecoverFromWalkingAsync()
	{
		walkingRecoveryInProgress = true;
		try
		{
			LifespanResponseDto response = await LifespanApiClient.WalkingRecoveryAsync();
			UserSession.ApplyLifespanResponse(response);
			UserSession.UpdateWalkingRecoveryProgress(0);
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
