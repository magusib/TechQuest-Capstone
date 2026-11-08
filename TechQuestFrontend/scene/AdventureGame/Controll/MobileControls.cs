using Godot;

public partial class MobileControls : CanvasLayer
{
    private Button jumpButton;

    public override void _Ready()
    {
        if (OS.GetName() != "Android")
        {
            Hide();
            return;
        }

        jumpButton = GetNodeOrNull<Button>("MobileControls/JumpButton");
        if (jumpButton == null)
        {
            GD.PrintErr("MobileControls: JumpButton was not found.");
            return;
        }

        jumpButton.ButtonDown += () => Input.ActionPress("jump");
        jumpButton.ButtonUp += () => Input.ActionRelease("jump");
    }

    public override void _ExitTree()
    {
        Input.ActionRelease("jump");
    }
}
