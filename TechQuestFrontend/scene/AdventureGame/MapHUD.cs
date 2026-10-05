using Godot;

public partial class MapHUD : Control
{
	private Label coinsLabel;
	private Label pointsLabel;

	public override void _Ready()
	{
		coinsLabel = GetNode<Label>("TopRight/CoinsPanel/CoinsLabel");
		pointsLabel = GetNode<Label>("TopRight/PointsPanel/PointsLabel");

		GetNode<Button>("TopLeft/HomeButton").Pressed += OnHomePressed;
		GetNode<Button>("TopLeft/QuestButton").Pressed += OnQuestPressed;
		RefreshCurrencyLabels();
	}

	public override void _Process(double delta)
	{
		if (coinsLabel.Text != UserSession.Coins.ToString())
			coinsLabel.Text = UserSession.Coins.ToString();

		if (pointsLabel.Text != UserSession.Points.ToString())
			pointsLabel.Text = UserSession.Points.ToString();
	}

	private void RefreshCurrencyLabels()
	{
		coinsLabel.Text = UserSession.Coins.ToString();
		pointsLabel.Text = UserSession.Points.ToString();
	}

	private void OnHomePressed()
	{
		GetTree().ChangeSceneToFile(TechQuest.Core.DashboardNavigationPaths.StudentDashboard);
	}

	private void OnQuestPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/Modules/Storyline/StudentStoryline.tscn");
	}
}
