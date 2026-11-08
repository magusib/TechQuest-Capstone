using Godot;

/// <summary>
/// Keeps desktop-authored controls usable on Android without changing the
/// existing desktop layout or button artwork.
/// </summary>
public partial class MobileUiCompatibility : Node
{
    private static bool IsAndroid => OS.GetName() == "Android";

    public override void _Ready()
    {
        if (!IsAndroid)
            return;

        GetTree().SceneChanged += OnSceneChanged;
        CallDeferred(nameof(PrepareCurrentScene));
    }

    private void OnSceneChanged()
    {
        CallDeferred(nameof(PrepareCurrentScene));
    }

    private void PrepareCurrentScene()
    {
        Node scene = GetTree().CurrentScene;
        if (GodotObject.IsInstanceValid(scene))
            PrepareNode(scene);
    }

    private static void PrepareNode(Node node)
    {
        if (node is BaseButton button)
        {
            // Touch does not need keyboard focus, and focus rings can cover
            // pixel-art button skins on Android.
            button.FocusMode = Control.FocusModeEnum.None;
            button.MouseFilter = Control.MouseFilterEnum.Stop;

            Vector2 minimum = button.CustomMinimumSize;
            minimum.X = Mathf.Max(minimum.X, 48f);
            minimum.Y = Mathf.Max(minimum.Y, 48f);
            button.CustomMinimumSize = minimum;
        }
        else if (node is LineEdit lineEdit)
        {
            Vector2 minimum = lineEdit.CustomMinimumSize;
            minimum.Y = Mathf.Max(minimum.Y, 48f);
            lineEdit.CustomMinimumSize = minimum;
        }
        else if (node is TextEdit textEdit)
        {
            Vector2 minimum = textEdit.CustomMinimumSize;
            minimum.Y = Mathf.Max(minimum.Y, 96f);
            textEdit.CustomMinimumSize = minimum;
        }

        foreach (Node child in node.GetChildren())
            PrepareNode(child);
    }
}
