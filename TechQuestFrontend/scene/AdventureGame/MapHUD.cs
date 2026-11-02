using Godot;
using System;

public partial class MapHUD : Control
{
	private Label coinsLabel;
	private Label pointsLabel;
	private Label lifespanLabel;
	private ProgressBar lifespanProgressBar;
	private Button restoreLifespanButton;
	private Control purchasePanel;
	private Label currentLifespanLabel;
	private Label notificationLabel;
	private bool purchaseInProgress;

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
		lifespanLabel = GetNodeOrNull<Label>("TopRight/LifespanPanel/LifespanLabel");
		lifespanProgressBar = GetNodeOrNull<ProgressBar>("TopRight/LifespanPanel/LifespanProgressBar");
		restoreLifespanButton = GetNodeOrNull<Button>("TopRight/LifespanPanel/RestoreLifespanButton");
		purchasePanel = GetNodeOrNull<Control>("LifespanPurchasePanel");
		currentLifespanLabel = GetNodeOrNull<Label>("LifespanPurchasePanel/MarginContainer/VBoxContainer/CurrentLifespanLabel");
		notificationLabel = GetNodeOrNull<Label>("LifespanPurchasePanel/MarginContainer/VBoxContainer/NotificationLabel");
		if (restoreLifespanButton != null)
			restoreLifespanButton.Pressed += OpenPurchasePanel;
		Button restoreButton = GetNodeOrNull<Button>("LifespanPurchasePanel/MarginContainer/VBoxContainer/RestoreButton");
		Button closeButton = GetNodeOrNull<Button>("LifespanPurchasePanel/MarginContainer/VBoxContainer/CloseButton");
		if (restoreButton != null)
			restoreButton.Pressed += PurchaseLifespan;
		if (closeButton != null)
			closeButton.Pressed += () => purchasePanel?.Hide();

		GetNode<Button>("TopLeft/HomeButton").Pressed += OnHomePressed;
		GetNode<Button>("TopLeft/QuestButton").Pressed += OnQuestPressed;
		RefreshCurrencyLabels();
		RefreshLifespanAsync();
	}

	public override void _Process(double delta)
	{
		if (!UserSession.IsStudent || coinsLabel == null || pointsLabel == null)
			return;

		if (coinsLabel.Text != UserSession.Coins.ToString())
			coinsLabel.Text = UserSession.Coins.ToString();

		if (pointsLabel.Text != UserSession.Points.ToString())
			pointsLabel.Text = UserSession.Points.ToString();
		RefreshLifespanLabels();
	}

	private void RefreshCurrencyLabels()
	{
		coinsLabel.Text = UserSession.Coins.ToString();
		pointsLabel.Text = UserSession.Points.ToString();
	}

	private void RefreshLifespanLabels()
	{
		if (lifespanLabel != null)
			lifespanLabel.Text = $"Lifespan: {UserSession.Lifespan} / {UserSession.MaximumLifespan}";
		if (lifespanProgressBar != null)
			lifespanProgressBar.Value = UserSession.Lifespan;
		if (currentLifespanLabel != null)
			currentLifespanLabel.Text = $"Current Lifespan: {UserSession.Lifespan} / {UserSession.MaximumLifespan}";
	}

	private async void RefreshLifespanAsync()
	{
		try
		{
			LifespanResponseDto response = await LifespanApiClient.GetAsync();
			UserSession.UpdateLifespan(response.Lifespan);
			UserSession.UpdateStudentProfile(response.TotalCoins, UserSession.Points);
			RefreshLifespanLabels();
			RefreshCurrencyLabels();
		}
		catch (Exception exception)
		{
			GD.PrintErr("Could not load lifespan: " + exception.Message);
		}
	}

	private void OpenPurchasePanel()
	{
		if (purchasePanel == null)
			return;
		purchasePanel.Show();
		if (notificationLabel != null)
			notificationLabel.Hide();
		RefreshLifespanLabels();
	}

	private async void PurchaseLifespan()
	{
		if (purchaseInProgress || UserSession.Lifespan >= UserSession.MaximumLifespan)
			return;

		purchaseInProgress = true;
		try
		{
			LifespanResponseDto response = await LifespanApiClient.PurchaseAsync();
			UserSession.UpdateLifespan(response.Lifespan);
			UserSession.UpdateStudentProfile(response.TotalCoins, UserSession.Points);
			if (notificationLabel != null)
		{
			notificationLabel.Text = "Lifespan restored successfully.";
			notificationLabel.Show();
		}
			RefreshCurrencyLabels();
			RefreshLifespanLabels();
		}
		catch (Exception exception)
		{
			if (notificationLabel != null)
			{
				notificationLabel.Text = exception.Message;
				notificationLabel.Show();
			}
		}
		finally
		{
			purchaseInProgress = false;
		}
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
