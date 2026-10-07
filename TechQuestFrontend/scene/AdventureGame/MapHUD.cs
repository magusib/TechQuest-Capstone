using Godot;

public partial class MapHUD : Control
{
	private Label coinsLabel;
	private Label pointsLabel;

	public override void _Ready()
	{
		Visible = UserSession.IsStudent;
		if (!UserSession.IsStudent)
		{
			SetProcess(false);
			return;
		}

		coinsLabel = GetNode<Label>("TopRight/CoinsPanel/CoinsLabel");
		pointsLabel = GetNode<Label>("TopRight/PointsPanel/PointsLabel");

		GetNode<Button>("TopLeft/HomeButton").Pressed += OnHomePressed;
		GetNode<Button>("TopLeft/QuestButton").Pressed += OnQuestPressed;
		RefreshCurrencyLabels();
	}

	public override void _Process(double delta)
	{
		if (!UserSession.IsStudent || coinsLabel == null || pointsLabel == null)
			return;

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
		if (!UserSession.IsStudent)
			return;

		Node mapHudLayer = GetTree().CurrentScene?.GetNodeOrNull<Node>("MapHUDLayer");
		Control questPopup = mapHudLayer?.GetNodeOrNull<Control>("EasyMapQuest");
		if (questPopup == null)
			questPopup = mapHudLayer?.GetNodeOrNull<Control>("MediumMapQuest");
		if (questPopup == null)
			questPopup = mapHudLayer?.GetNodeOrNull<Control>("HardMapQuest");

		if (questPopup == null)
		{
			GD.PrintErr("Quest popup was not found in the current map.");
			return;
		}

		questPopup.Call("OpenForStudent");
	}
}
