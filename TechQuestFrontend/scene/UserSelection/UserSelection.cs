using Godot;

public partial class UserSelection : Control
{
	private Button studentButton;
	private Button professorButton;
	private Button adminButton;
	private Button backButton;

	public override void _Ready()
	{
		studentButton = GetNode<Button>("StudentButton");
		professorButton = GetNode<Button>("ProfessorButton");
		adminButton = GetNode<Button>("AdminButton");
		backButton = GetNode<Button>("BackButton");

		studentButton.Pressed += OnStudentPressed;
		professorButton.Pressed += OnProfessorPressed;
		adminButton.Pressed += OnAdminPressed;
		backButton.Pressed += OnBackPressed;
	}

	private void OnStudentPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/UserLogin/Student/StudentLogin.tscn");
	}

	private void OnProfessorPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/UserLogin/Professor/ProfessorLogin.tscn");
	}

	private void OnAdminPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/UserLogin/Admin/AdminLogin.tscn");
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/LandingPage/LandingPage.tscn");
	}
}
