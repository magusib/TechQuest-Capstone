using Godot;
using System;

public partial class StudentTutorial : Control
{
	private Button backButton;

	public override void _Ready()
	{
		backButton = GetNode<Button>("MainPanel/HeaderPanel/BackButton");

		backButton.Pressed += OnBackPressed;

	}

	private void OnBackPressed()
	{
		if (GetParent() is DashboardBase)
		{
			Hide();
			return;
		}

		GetTree().ChangeSceneToFile("res://scene/Dashboard/UserDashboard/Student Dashboard/StudentDashboard.tscn");
	}
}
