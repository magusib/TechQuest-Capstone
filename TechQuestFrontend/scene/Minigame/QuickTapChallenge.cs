using Godot;
using System;

public partial class QuickTapChallenge : Control
{
	[Export] public Texture2D TargetTexture { get; set; }
	[Export] public int TargetScore { get; set; } = 10;
	[Export] public int PointsPerTap { get; set; } = 1;
	[Export] public float TargetSize { get; set; } = 140;
	[Export] public float SpawnInterval { get; set; } = 0.45f;
	[Export] public float TargetDisplayDuration { get; set; } = 1.8f;
	[Export] public int GameDurationSeconds { get; set; } = 30;
	[Export] public int FailureCoinPenalty { get; set; } = 5;
	[Export] public PackedScene TapTargetScene { get; set; }

	private Control playArea;
	private Control targetContainer;
	private TapTarget targetButton;
	private Label scoreLabel;
	private Label targetScoreLabel;
	private Label currentScoreLabel;
	private Label timerLabel;
	private Label resultTitle;
	private Label resultMessage;
	private Label answerTipLabel;
	private Label coinPenaltyLabel;
	private Label statusLabel;
	private Button restartButton;
	private Button retryButton;
	private Control resultOverlay;
	private Timer countdownTimer;
	private Timer spawnTimer;
	private Timer targetLifetimeTimer;
	private int questionId;
	private int npcId;
	private int deploymentId;
	private string gameType = "QuickTapChallenge";
	private int timeLeft;
	private int timerSeconds = 30;
	private int score;
	private bool gameEnded;
	private bool resultConfirmed;
	private bool isSubmitting;
	private bool targetNeedsPosition;
	private bool attemptWon;
	private Guid attemptId;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		playArea = GetNode<Control>("MainPanel/MainMargin/MainLayout/PlayArea");
		targetContainer = GetNode<Control>("MainPanel/MainMargin/MainLayout/PlayArea/TargetContainer");
		scoreLabel = GetNode<Label>("MainPanel/MainMargin/MainLayout/Header/ScoreContainer/ScoreLabel");
		targetScoreLabel = GetNode<Label>("MainPanel/MainMargin/MainLayout/ProgressContainer/TargetScoreLabel");
		currentScoreLabel = GetNode<Label>("MainPanel/MainMargin/MainLayout/ProgressContainer/CurrentScoreLabel");
		timerLabel = GetNode<Label>("MainPanel/MainMargin/MainLayout/Header/TimerContainer/TimerLabel");
		statusLabel = GetNode<Label>("MainPanel/MainMargin/MainLayout/BottomPanel/BottomLayout/StatusLabel");
		resultOverlay = GetNode<Control>("ResultOverlay");
		resultTitle = GetNode<Label>("ResultOverlay/ResultPanel/ResultLayout/ResultTitle");
		resultMessage = GetNode<Label>("ResultOverlay/ResultPanel/ResultLayout/ResultMessage");
		answerTipLabel = GetNode<Label>("ResultOverlay/ResultPanel/ResultLayout/AnswerTipLabel");
		coinPenaltyLabel = GetNode<Label>("ResultOverlay/ResultPanel/ResultLayout/CoinPenaltyLabel");
		restartButton = GetNode<Button>("MainPanel/MainMargin/MainLayout/BottomPanel/BottomLayout/RestartButton");
		retryButton = GetNode<Button>("ResultOverlay/ResultPanel/ResultLayout/RetryResultButton");
		countdownTimer = GetNode<Timer>("CountdownTimer");
		spawnTimer = GetNode<Timer>("SpawnTimer");
		targetLifetimeTimer = GetNode<Timer>("TargetLifetimeTimer");

		Button closeButton = GetNode<Button>("MainPanel/MainMargin/MainLayout/Header/CloseButton");
		Button playAgainButton = GetNode<Button>("ResultOverlay/ResultPanel/ResultLayout/PlayAgainButton");
		Button proceedButton = GetNode<Button>("ResultOverlay/ResultPanel/ResultLayout/ResultCloseButton");
		closeButton.Pressed += OnClosePressed;
		restartButton.Pressed += StartGame;
		playAgainButton.Pressed += StartGame;
		proceedButton.Pressed += OnClosePressed;
		if (questionId > 0)
		{
			closeButton.Hide();
			restartButton.Hide();
			playAgainButton.Hide();
			proceedButton.Hide();
			proceedButton.Text = "Proceed to Question";
		}
		retryButton.Pressed += RetryResult;
		countdownTimer.Timeout += OnCountdownTimeout;
		spawnTimer.Timeout += OnSpawnTimeout;
		targetLifetimeTimer.Timeout += OnTargetLifetimeTimeout;
		countdownTimer.ProcessMode = ProcessModeEnum.Always;
		spawnTimer.ProcessMode = ProcessModeEnum.Always;
		targetLifetimeTimer.ProcessMode = ProcessModeEnum.Always;
		if (questionId == 0)
			StartGame();
	}

	public void Configure(int assignedDeploymentId, int assignedNpcId, int assignedQuestionId, string assignedGameType, int assignedTimerSeconds, Guid assignedAttemptId)
	{
		deploymentId = assignedDeploymentId;
		npcId = assignedNpcId;
		questionId = assignedQuestionId;
		gameType = assignedGameType;
		timerSeconds = 30;
		attemptId = assignedAttemptId;
	}

	public void StartAssignedGame() => StartGame();

	public void StopGame()
	{
		gameEnded = true;
		countdownTimer?.Stop();
		spawnTimer?.Stop();
		targetLifetimeTimer?.Stop();
		RemoveTarget();
	}

	public override void _Process(double delta)
	{
		if (targetNeedsPosition && playArea.Size.X > 0 && playArea.Size.Y > 0)
		{
			MoveTarget();
			targetNeedsPosition = false;
		}
	}

	private void StartGame()
	{
		if (isSubmitting || (gameEnded && questionId > 0 && !resultConfirmed))
			return;

		if (TapTargetScene == null)
		{
			gameEnded = true;
			statusLabel.Text = "Assign the reusable TapTarget scene in the Inspector.";
			return;
		}

		spawnTimer.Stop();
		targetLifetimeTimer.Stop();
		RemoveTarget();
		score = 0;
		timeLeft = questionId > 0 ? timerSeconds : Math.Max(1, GameDurationSeconds);
		gameEnded = false;
		resultConfirmed = false;
		attemptWon = false;
		if (questionId == 0)
			attemptId = Guid.NewGuid();
		resultOverlay.Hide();
		retryButton.Hide();
		statusLabel.Text = "Tap the character before it disappears!";
		UpdateStatus();
		countdownTimer.Start();
		SpawnTarget();
	}

	private void SpawnTarget()
	{
		if (gameEnded || TapTargetScene == null)
			return;
		if (targetButton != null && GodotObject.IsInstanceValid(targetButton))
			return;

		TapTarget spawnedTarget = TapTargetScene.Instantiate<TapTarget>();
		if (TargetTexture != null)
			spawnedTarget.TargetTexture = TargetTexture;
		spawnedTarget.TargetSize = Math.Max(1, TargetSize);
		spawnedTarget.Pressed += () => OnTargetPressed(spawnedTarget);
		targetButton = spawnedTarget;
		targetContainer.AddChild(spawnedTarget);
		targetNeedsPosition = true;
		targetLifetimeTimer.WaitTime = Math.Max(0.1f, TargetDisplayDuration);
		targetLifetimeTimer.Start();
	}

	private void MoveTarget()
	{
		if (targetButton == null || !GodotObject.IsInstanceValid(targetButton))
			return;

		Vector2 size = targetButton.Size;
		float maxX = Math.Max(0, playArea.Size.X - size.X);
		float maxY = Math.Max(0, playArea.Size.Y - size.Y);
		targetButton.Position = new Vector2((float)GD.RandRange(0, maxX), (float)GD.RandRange(0, maxY));
	}

	private void OnTargetPressed(TapTarget pressedTarget)
	{
		if (gameEnded || pressedTarget != targetButton || pressedTarget.Disabled)
			return;

		pressedTarget.Disabled = true;
		score += Math.Max(1, PointsPerTap);
		UpdateStatus();
		targetLifetimeTimer.Stop();
		RemoveTarget();
		if (score >= Math.Max(1, TargetScore))
		{
			FinishGame(true);
			return;
		}
		spawnTimer.WaitTime = Math.Max(0.05f, SpawnInterval);
		spawnTimer.Start();
	}

	private void OnSpawnTimeout() => SpawnTarget();

	private void OnTargetLifetimeTimeout()
	{
		RemoveTarget();
		if (!gameEnded)
		{
			spawnTimer.WaitTime = Math.Max(0.05f, SpawnInterval);
			spawnTimer.Start();
		}
	}

	private void RemoveTarget()
	{
		targetNeedsPosition = false;
		if (targetButton != null && GodotObject.IsInstanceValid(targetButton))
		{
			targetButton.Disabled = true;
			targetButton.Hide();
			targetButton.QueueFree();
		}
		targetButton = null;
	}

	private void OnCountdownTimeout()
	{
		if (gameEnded)
			return;

		timeLeft--;
		UpdateStatus();
		if (timeLeft <= 0)
			FinishGame(false);
	}

	private void UpdateStatus()
	{
		scoreLabel.Text = score + " / " + Math.Max(1, TargetScore);
		targetScoreLabel.Text = "Target score: " + Math.Max(1, TargetScore);
		currentScoreLabel.Text = "Current score: " + score;
		timerLabel.Text = timeLeft.ToString();
	}

	private void FinishGame(bool won)
	{
		if (gameEnded)
			return;

		gameEnded = true;
		attemptWon = won;
		countdownTimer.Stop();
		spawnTimer.Stop();
		targetLifetimeTimer.Stop();
		RemoveTarget();
		resultOverlay.Show();
		resultTitle.Text = won ? "Great tapping!" : "Time is up!";
		resultMessage.Text = won ? "Confirming your answer tip..." : "Confirming the coin penalty...";
		answerTipLabel.Text = "";
		coinPenaltyLabel.Text = "";
		restartButton.Disabled = questionId > 0;
		if (npcId > 0 && questionId > 0)
			SubmitResult(won);
		else
		{
			resultMessage.Text = "Practice round complete. Launch from an assigned NPC to record rewards.";
			restartButton.Disabled = false;
			resultConfirmed = true;
		}
	}

	private async void SubmitResult(bool won)
	{
		if (isSubmitting || resultConfirmed)
			return;

		isSubmitting = true;
		retryButton.Disabled = true;
		try
		{
			MiniGameAttemptResultDto result = await MiniGameApiClient.SubmitAttemptAsync(
				attemptId, deploymentId, questionId, gameType, won);
			UserSession.UpdateStudentProfile(result.TotalCoins, result.TotalPoints);
			resultConfirmed = true;
			retryButton.Hide();
			answerTipLabel.Text = string.IsNullOrWhiteSpace(result.AnswerTip)
				? "No answer tip has been configured for this NPC."
				: "Answer tip: " + result.AnswerTip;
			coinPenaltyLabel.Text = result.CoinsDeducted > 0
				? result.CoinsDeducted + " coins deducted. Coins remaining: " + result.TotalCoins
				: "No coins deducted.";
			resultMessage.Text = "Mini-game result confirmed.";
			if (questionId > 0)
			{
				restartButton.Hide();
				GetNode<Button>("ResultOverlay/ResultPanel/ResultLayout/PlayAgainButton").Hide();
				Button proceedButton = GetNode<Button>("ResultOverlay/ResultPanel/ResultLayout/ResultCloseButton");
				proceedButton.Text = "Proceed to Question";
				proceedButton.Show();
			}
			else
				restartButton.Disabled = false;
		}
		catch (Exception exception)
		{
			resultMessage.Text = "The result could not be confirmed. " + exception.Message +
				" Retry to safely check the same attempt.";
			retryButton.Show();
			restartButton.Disabled = true;
		}
		finally
		{
			isSubmitting = false;
			retryButton.Disabled = false;
		}
	}

	private void RetryResult() => SubmitResult(attemptWon);
	private void OnClosePressed()
	{
		if (questionId > 0)
		{
			if (resultConfirmed)
				MiniGameLauncher.ProceedToQuestion(this, deploymentId);
			return;
		}

		MiniGameLauncher.CloseOverlay(this);
	}
}
