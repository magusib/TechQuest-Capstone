using Godot;

public partial class AdminCamera2d : Camera2D
{
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

		if (Input.IsActionPressed("move_left"))
		{
			GlobalPosition += new Vector2(-10, 0);
		}

		if (Input.IsActionPressed("move_right"))
		{
			GlobalPosition += new Vector2(10, 0);
		}
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
