using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class UserProgress : Control
{
    private readonly Dictionary<string, (ScrollContainer Screen, VBoxContainer Content)> screens = new();
    private PackedScene itemScene;

    public override void _Ready()
    {
        if (!UserSession.IsStudent)
        {
            QueueFree();
            return;
        }

        itemScene = GD.Load<PackedScene>("res://scene/Dashboard/UserDashboard/Student Dashboard/UserProgressItem.tscn");
        screens["Easy"] = (GetNode<ScrollContainer>("MainPanel/MainMargin/MainVBox/DifficultyScreens/EasyScreen"), GetNode<VBoxContainer>("MainPanel/MainMargin/MainVBox/DifficultyScreens/EasyScreen/EasyContent"));
        screens["Medium"] = (GetNode<ScrollContainer>("MainPanel/MainMargin/MainVBox/DifficultyScreens/MediumScreen"), GetNode<VBoxContainer>("MainPanel/MainMargin/MainVBox/DifficultyScreens/MediumScreen/MediumContent"));
        screens["Hard"] = (GetNode<ScrollContainer>("MainPanel/MainMargin/MainVBox/DifficultyScreens/HardScreen"), GetNode<VBoxContainer>("MainPanel/MainMargin/MainVBox/DifficultyScreens/HardScreen/HardContent"));

        GetNode<Button>("MainPanel/MainMargin/MainVBox/DifficultyNavigation/EasyButton").Pressed += () => ShowDifficulty("Easy");
        GetNode<Button>("MainPanel/MainMargin/MainVBox/DifficultyNavigation/MediumButton").Pressed += () => ShowDifficulty("Medium");
        GetNode<Button>("MainPanel/MainMargin/MainVBox/DifficultyNavigation/HardButton").Pressed += () => ShowDifficulty("Hard");
        GetNode<Button>("MainPanel/MainMargin/MainVBox/Header/CloseButton").Pressed += Close;
        ShowDifficulty("Easy");
    }

    private void ShowDifficulty(string difficulty)
    {
        foreach (var entry in screens)
            entry.Value.Screen.Visible = entry.Key == difficulty;
        LoadDifficultyAsync(difficulty);
    }

    private async void LoadDifficultyAsync(string difficulty)
    {
        VBoxContainer content = screens[difficulty].Content;
        foreach (Node child in content.GetChildren())
            child.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        try
        {
            List<UserProgressItemDto> history = await UserProgressApiClient.GetAsync(difficulty);
            if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
            if (history.Count == 0)
            {
                content.AddChild(new Label { Text = "No completed " + difficulty + " quizzes yet.", AutowrapMode = TextServer.AutowrapMode.Word });
                return;
            }
            foreach (UserProgressItemDto item in history)
            {
                UserProgressItem card = itemScene.Instantiate<UserProgressItem>();
                card.SetItem(item);
                content.AddChild(card);
            }
        }
        catch (Exception exception)
        {
            content.AddChild(new Label { Text = "Could not load User Progress.", AutowrapMode = TextServer.AutowrapMode.Word });
            GD.PrintErr("User Progress load failed: " + exception.Message);
        }
    }

    private void Close()
    {
        Node profile = GetParent()?.GetNodeOrNull("StudentProfilePopup");
        if (profile is Control control) control.Show();
        QueueFree();
    }
}
