using Godot;

public partial class LandingPage : Control
{
	private Button loginButton;
	private Button exitButton;

	public override void _Ready()
	{
		loginButton = GetNode<Button>("Login");
		exitButton = GetNode<Button>("Exit");

		loginButton.Pressed += OnLoginPressed;
		exitButton.Pressed += OnExitPressed;
	}

	private void OnLoginPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/UserSelection/UserSelection.tscn");
	}

	private void OnExitPressed()
	{
		GetTree().Quit();
	}
}
