using Godot;

public partial class MainPage : Control
{
	private ProgressBar loadingBar;

	public override void _Ready()
	{
		// Get the ProgressBar node
		loadingBar = GetNode<ProgressBar>("ProgressBar");
	}

	public override void _Process(double delta)
	{
		// Increase loading bar
		if (loadingBar.Value < 100)
		{
			loadingBar.Value += 25 * delta;
		}
		else
		{
			// Go to Landing Page
			GetTree().ChangeSceneToFile(
                "res://scene/LandingPage/LandingPage.tscn"
			);
		}
	}
}
