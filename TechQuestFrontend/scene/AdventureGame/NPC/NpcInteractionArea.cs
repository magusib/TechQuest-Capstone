using Godot;

public partial class NpcInteractionArea : Area2D
{
	public override void _Ready()
	{
		InputPickable = true;
		CollisionLayer = 1;
		CollisionMask = 0;
		InputEvent += OnInputEvent;
	}

	private void OnInputEvent(Node viewport, InputEvent inputEvent, long shapeIndex)
	{
		if (!UserSession.IsStudent ||
			!GetParent().HasMeta("NpcId") ||
			!GetParent().HasMeta("DeploymentId"))
			return;

		bool selected = false;
		if (inputEvent is InputEventMouseButton mouseEvent)
			selected = mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left;
		else if (inputEvent is InputEventScreenTouch touchEvent)
			selected = touchEvent.Pressed;

		if (selected)
		{
			MiniGameLauncher.OpenForDeployment(
				this,
				GetParent().GetMeta("DeploymentId").AsInt32());
			GetViewport().SetInputAsHandled();
		}
	}
}
