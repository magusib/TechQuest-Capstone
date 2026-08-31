using Godot;
using System;

public partial class ProfessorLeaderboard : Control
{
	private Button backButton;

	public override void _Ready()
	{
		backButton = GetNode<Button>("BackButton");
		
		backButton.Pressed += OnBackPressed;

	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/UserDashboard/Professor Dashboard/ProfessorDashboard.tscn");
	}
}
