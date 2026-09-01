using Godot;
using System;

public partial class UserManagement : Control
{
	private Button backButton;
	private Button professorButton;

	public override void _Ready()
	{
		backButton = GetNode<Button>("BackButton");
		professorButton = GetNode<Button>("ProfessorButton");
		
		
		backButton.Pressed += OnBackPressed;
		professorButton.Pressed += onProfessorPressed;
		

	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/UserDashboard/Admin Dashboard/AdminDashboard.tscn");
	}
	
	private void onProfessorPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/UserManagement/ProfessorManagement.tscn");
	}
}
