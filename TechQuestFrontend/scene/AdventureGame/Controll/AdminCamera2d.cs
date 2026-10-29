using Godot;

public partial class AdminCamera2d : Camera2D
{
	private const float CameraSpeed = 600f;

	public override void _Ready()
	{
		UpdateCameraState();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!UserSession.IsAdmin)
		{
			Enabled = false;
			SetPhysicsProcess(false);
			return;
		}

		Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		GlobalPosition += direction * CameraSpeed * (float)delta;
	}

	private void UpdateCameraState()
	{
		if (!UserSession.IsAdmin)
		{
			Enabled = false;
			Visible = false;
			SetProcess(false);
			SetPhysicsProcess(false);
			return;
		}

		Enabled = true;
		Visible = true;
		SetProcess(true);
		SetPhysicsProcess(true);
	}
}
