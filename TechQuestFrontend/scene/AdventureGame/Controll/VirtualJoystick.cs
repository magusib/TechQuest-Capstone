using Godot;

public partial class VirtualJoystick : Control
{
    [Export] public float DeadZone { get; set; } = 0.18f;
    private bool pressed;
    private float axis;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 center = Size / 2.0f;
        float radius = Mathf.Min(Size.X, Size.Y) * 0.42f;
        float knobOffset = axis * radius * 0.55f;
        DrawCircle(center, radius, new Color(0.08f, 0.12f, 0.2f, 0.42f));
        DrawArc(center, radius, 0, Mathf.Tau, 48, new Color(0.7f, 0.9f, 1f, 0.8f), 4f);
        DrawCircle(center + new Vector2(knobOffset, 0), radius * 0.48f,
            new Color(0.25f, 0.65f, 1f, 0.86f));
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventScreenTouch touch)
        {
            if (touch.Pressed)
            {
                pressed = true;
                UpdateAxis(touch.Position);
                AcceptEvent();
            }
            else if (pressed)
            {
                ResetJoystick();
                AcceptEvent();
            }
        }
        else if (@event is InputEventScreenDrag drag && pressed)
        {
            UpdateAxis(drag.Position);
            AcceptEvent();
        }
        else if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (mouseButton.Pressed)
            {
                pressed = true;
                UpdateAxis(mouseButton.Position);
            }
            else if (pressed)
            {
                ResetJoystick();
            }
            AcceptEvent();
        }
        else if (@event is InputEventMouseMotion motion && pressed)
        {
            UpdateAxis(motion.Position);
            AcceptEvent();
        }
    }

    private void UpdateAxis(Vector2 position)
    {
        float halfWidth = Mathf.Max(1f, Size.X / 2.0f);
        axis = Mathf.Clamp((position.X - halfWidth) / halfWidth, -1f, 1f);
        if (axis < -DeadZone) Input.ActionPress("move_left");
        else Input.ActionRelease("move_left");
        if (axis > DeadZone) Input.ActionPress("move_right");
        else Input.ActionRelease("move_right");
        QueueRedraw();
    }

    private void ResetJoystick()
    {
        pressed = false;
        axis = 0f;
        Input.ActionRelease("move_left");
        Input.ActionRelease("move_right");
        QueueRedraw();
    }
}
