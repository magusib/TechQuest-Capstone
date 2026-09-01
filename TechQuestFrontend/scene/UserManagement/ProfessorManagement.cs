using Godot;
using System;

public partial class ProfessorManagement : Control
{
	private Button backButton;
	private Button professorButton;
	private Button studentButton;
	private Button registerButton;

	public override void _Ready()
	{
		backButton = GetNode<Button>("BackButton");
		professorButton = GetNode<Button>("ProfessorButton");
		studentButton = GetNode<Button>("StudentButton");
		registerButton = GetNode<Button>("RegisterButton");
		
		
		backButton.Pressed += OnBackPressed;
		professorButton.Pressed += onProfessorPressed;
		studentButton.Pressed += onStudentPressed;
		registerButton.Pressed += onRegisterPressed;

		

	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/UserDashboard/Admin Dashboard/AdminDashboard.tscn");
	}
	
	private void onProfessorPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/UserManagement/ProfessorManagement.tscn");
	}
	
	private void onStudentPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/UserManagement/UserManagement.tscn");
	}
	
	private void onRegisterPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/UserManagement/ProfessorRegister.tscn");
	}
}
