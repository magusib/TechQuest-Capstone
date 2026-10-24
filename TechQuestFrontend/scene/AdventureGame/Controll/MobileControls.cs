using Godot;

public partial class MobileControls : CanvasLayer
{
    public override void _Ready()
    {
        if (OS.GetName() != "Android")
        {
            Hide();
            return;
        }

        Button jumpButton = GetNode<Button>("MobileControls/JumpButton");
        jumpButton.ButtonDown += () => Input.ActionPress("jump");
        jumpButton.ButtonUp += () => Input.ActionRelease("jump");
    }
}
