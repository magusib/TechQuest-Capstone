using Godot;

public partial class NPC : StaticBody2D
{
	[Export] public int NpcId { get; set; }
	[Export] public string NpcName { get; set; } = "";
	[Export] public string SubjectName { get; set; } = "";

	private Sprite2D highlightSprite;
	private Area2D interactionArea;

	private bool isSelected = false;
	private bool studentInRange = false;

	public override void _Ready()
	{
		highlightSprite = GetNodeOrNull<Sprite2D>("HighlightSprite");
		interactionArea = GetNodeOrNull<Area2D>("InteractionArea");

		if (highlightSprite != null)
			highlightSprite.Visible = false;

		if (interactionArea != null)
		{
			interactionArea.BodyEntered += OnInteractionAreaBodyEntered;
			interactionArea.BodyExited += OnInteractionAreaBodyExited;
		}

		InputEvent += OnNpcInputEvent;
	}

	private void OnNpcInputEvent(
		Node viewport,
		InputEvent @event,
		long shapeIdx)
	{
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left &&
				mouseButton.Pressed)
			{
				SelectNPC();
			}
		}
	}

	private void SelectNPC()
	{
		MapInteractionController controller =
			GetTree().GetFirstNodeInGroup(
                "InteractionController"
			) as MapInteractionController;

		if (controller == null)
		{
			GD.PrintErr("MapInteractionController not found.");
			return;
		}

		controller.SelectNPC(this);
	}

	public void SetSelected(bool selected)
	{
		isSelected = selected;

		if (highlightSprite != null)
			highlightSprite.Visible = selected;
	}

	public bool IsSelected()
	{
		return isSelected;
	}

	public bool IsStudentInRange()
	{
		return studentInRange;
	}

	private void OnInteractionAreaBodyEntered(Node2D body)
	{
		if (IsStudent(body))
		{
			studentInRange = true;

			MapInteractionController controller =
				GetTree().GetFirstNodeInGroup(
                    "InteractionController"
				) as MapInteractionController;

			controller?.UpdateInteractionButton();
		}
	}

	private void OnInteractionAreaBodyExited(Node2D body)
	{
		if (IsStudent(body))
		{
			studentInRange = false;

			MapInteractionController controller =
				GetTree().GetFirstNodeInGroup(
                    "InteractionController"
				) as MapInteractionController;

			controller?.UpdateInteractionButton();
		}
	}

	private bool IsStudent(Node body)
	{
		return body.IsInGroup("Student");
	}
}
