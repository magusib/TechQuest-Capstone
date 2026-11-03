using Godot;
using System;

public partial class MapHUD : Control
{
	private Label coinsLabel;
	private Label pointsLabel;
	private Label lifespanLabel;
	private ProgressBar lifespanProgressBar;
	private Control lifespanBottle;
	private Control lifespanFill;
	private Label lifespanPercentageLabel;
	private Button restoreLifespanButton;
	private Control purchasePanel;
	private Label currentLifespanLabel;
	private Label notificationLabel;
	private Label fullRestoreTimerLabel;
	private Label walkingRestoreTimerLabel;
	private ConfirmationDialog purchaseConfirmationDialog;
	private AcceptDialog purchaseMessageDialog;
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
		lifespanBottle = GetNodeOrNull<Control>("LeftLifespanPanel/LifespanVBox/LifespanBottle");
		lifespanFill = GetNodeOrNull<Control>("LeftLifespanPanel/LifespanVBox/LifespanBottle/LifespanFill");
		lifespanPercentageLabel = GetNodeOrNull<Label>("LeftLifespanPanel/LifespanVBox/LifespanPercentageLabel");
		restoreLifespanButton = GetNodeOrNull<Button>("LeftLifespanPanel/LifespanVBox/RestoreButton")
			?? GetNodeOrNull<Button>("TopRight/LifespanPanel/RestoreLifespanButton");
		purchasePanel = GetNodeOrNull<Control>("../LifespanPurchasePanel");
		currentLifespanLabel = GetNodeOrNull<Label>("../LifespanPurchasePanel/MarginContainer/VBoxContainer/CurrentLifespanLabel");
		notificationLabel = GetNodeOrNull<Label>("../LifespanPurchasePanel/MarginContainer/VBoxContainer/NotificationLabel");
		fullRestoreTimerLabel = GetNodeOrNull<Label>("../LifespanPurchasePanel/MarginContainer/VBoxContainer/FullRestoreTimerLabel");
		walkingRestoreTimerLabel = GetNodeOrNull<Label>("../LifespanPurchasePanel/MarginContainer/VBoxContainer/WalkingRestoreTimerLabel");
		if (restoreLifespanButton != null)
			restoreLifespanButton.Pressed += OpenPurchasePanel;
		Button restoreButton = GetNodeOrNull<Button>("../LifespanPurchasePanel/MarginContainer/VBoxContainer/RestoreButton");
		Button closeButton = GetNodeOrNull<Button>("../LifespanPurchasePanel/MarginContainer/VBoxContainer/CloseButton");
		if (restoreButton != null)
			restoreButton.Pressed += ConfirmPurchaseLifespan;
		if (closeButton != null)
			closeButton.Pressed += () => purchasePanel?.Hide();

		CreatePurchaseDialogs();

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
		if (lifespanPercentageLabel != null)
			lifespanPercentageLabel.Text = $"{GetLifespanPercentage()}%";
		RefreshLifespanFill();
		if (currentLifespanLabel != null)
			currentLifespanLabel.Text = $"Current Lifespan: {UserSession.Lifespan} / {UserSession.MaximumLifespan}";
		if (fullRestoreTimerLabel != null)
			fullRestoreTimerLabel.Text = $"Full restore: {FormatRemaining(GetRemaining(UserSession.LifespanAutoRecoveryAtUtc))}";
		if (walkingRestoreTimerLabel != null)
		{
			double remainingWalkingSeconds = Math.Max(0, 60 - UserSession.WalkingRecoverySeconds);
			walkingRestoreTimerLabel.Text = $"+1 while walking: {FormatSeconds(remainingWalkingSeconds)}";
		}
	}

	private int GetLifespanPercentage()
	{
		return Math.Clamp(
			(int)Math.Round(UserSession.Lifespan * 100.0 / UserSession.MaximumLifespan),
			0,
			100);
	}

	private void RefreshLifespanFill()
	{
		if (lifespanBottle == null || lifespanFill == null || lifespanBottle.Size.Y <= 0)
			return;

		const float inset = 3.0f;
		float innerWidth = Math.Max(0, lifespanBottle.Size.X - inset * 2);
		float innerHeight = Math.Max(0, lifespanBottle.Size.Y - inset * 2);
		float fillHeight = innerHeight * GetLifespanPercentage() / 100.0f;
		lifespanFill.Position = new Vector2(inset, inset + innerHeight - fillHeight);
		lifespanFill.Size = new Vector2(innerWidth, fillHeight);
	}

	private static TimeSpan GetRemaining(DateTimeOffset? target)
	{
		return target.HasValue
			? target.Value - DateTimeOffset.UtcNow
			: TimeSpan.Zero;
	}

	private static string FormatRemaining(TimeSpan remaining)
	{
		if (remaining <= TimeSpan.Zero)
			return "Ready / none";

		return $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
	}

	private static string FormatSeconds(double seconds)
	{
		int wholeSeconds = Math.Clamp((int)Math.Ceiling(seconds), 0, 60);
		return $"{wholeSeconds / 60:00}:{wholeSeconds % 60:00}";
	}

	private async void RefreshLifespanAsync()
	{
		try
		{
			LifespanResponseDto response = await LifespanApiClient.GetAsync();
			UserSession.ApplyLifespanResponse(response);
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

	private void CreatePurchaseDialogs()
	{
		purchaseConfirmationDialog = new ConfirmationDialog
		{
			Title = "Confirm Lifespan Purchase",
			OkButtonText = "Yes",
			CancelButtonText = "No"
		};
		purchaseConfirmationDialog.Confirmed += PurchaseLifespan;
		AddChild(purchaseConfirmationDialog);

		purchaseMessageDialog = new AcceptDialog
		{
			Title = "Lifespan Purchase"
		};
		AddChild(purchaseMessageDialog);
	}

	private void ConfirmPurchaseLifespan()
	{
		if (purchaseInProgress || UserSession.Lifespan >= UserSession.MaximumLifespan)
			return;

		if (UserSession.Coins < 500)
		{
			ShowPurchaseMessage(
				$"Not enough coins. You need 500 Coins, but you only have {UserSession.Coins}.",
				"Insufficient Coins");
			return;
		}

		purchaseConfirmationDialog.DialogText =
			$"Purchase full lifespan for 500 Coins?\n\nYour current coins: {UserSession.Coins}";
		purchaseConfirmationDialog.PopupCentered();
	}

	private async void PurchaseLifespan()
	{
		if (purchaseInProgress || UserSession.Lifespan >= UserSession.MaximumLifespan)
			return;

		purchaseInProgress = true;
		try
		{
			LifespanResponseDto response = await LifespanApiClient.PurchaseAsync();
			UserSession.ApplyLifespanResponse(response);
			ShowPurchaseMessage("Lifespan restored successfully!", "Purchase Successful");
			purchasePanel?.Hide();
			RefreshCurrencyLabels();
			RefreshLifespanLabels();
		}
		catch (Exception exception)
		{
			string message = exception.Message.Contains("Not enough Coins", StringComparison.OrdinalIgnoreCase)
				? "Not enough coins to purchase full lifespan. You need 500 Coins."
				: exception.Message;
			ShowPurchaseMessage(message, "Purchase Failed");
		}
		finally
		{
			purchaseInProgress = false;
		}
	}

	private void ShowPurchaseMessage(string message, string title)
	{
		if (purchaseMessageDialog == null)
			return;

		purchaseMessageDialog.Title = title;
		purchaseMessageDialog.DialogText = message;
		purchaseMessageDialog.PopupCentered();
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
