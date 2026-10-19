using Godot;
using System;

public partial class QuickTapChallenge : Control
{
	[Export]
	public Texture2D TargetTexture { get; set; }

	[Export]
	public Texture2D BackgroundTexture { get; set; }

	[Export]
	public Color BackgroundColor { get; set; } = new Color(0.98f, 0.67f, 0.35f);

	[Export]
	public int TargetScore { get; set; } = 10;

	[Export]
	public int PointsPerTap { get; set; } = 1;

	[Export]
	public float TargetSize { get; set; } = 140;

	[Export]
	public PackedScene TapTargetScene { get; set; }

	private Control playArea;
	private TapTarget targetButton;
	private Label scoreLabel;
	private Label timerLabel;
	private Label resultLabel;
	private Label resultDetails;
	private Button restartButton;
	private Button retryButton;
	private PanelContainer resultPanel;
	private Timer countdownTimer;
	private int questionId;
	private int npcId;
	private int deploymentId;
	private string gameType = "QuickTapChallenge";
	private int timeLeft;
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
		BuildInterface();
		if (questionId == 0)
			StartGame();
	}

	public void Configure(int assignedDeploymentId, int assignedNpcId, int assignedQuestionId, string assignedGameType)
	{
		deploymentId = assignedDeploymentId;
		npcId = assignedNpcId;
		questionId = assignedQuestionId;
		gameType = assignedGameType;
	}

	public void StopGame()
	{
		gameEnded = true;
		countdownTimer.Stop();
		targetButton.Disabled = true;
	}

	public override void _Process(double delta)
	{
		if (targetNeedsPosition && playArea.Size.X > 0 && playArea.Size.Y > 0)
		{
			MoveTarget();
			targetNeedsPosition = false;
		}
	}

	private void BuildInterface()
	{
		if (BackgroundTexture != null)
		{
			TextureRect background = new()
			{
				Texture = BackgroundTexture,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale,
				MouseFilter = MouseFilterEnum.Ignore
			};
			MiniGameUi.Fill(background);
			AddChild(background);
		}
		else
		{
			ColorRect background = new()
			{
				Color = BackgroundColor,
				MouseFilter = MouseFilterEnum.Ignore
			};
			MiniGameUi.Fill(background);
			AddChild(background);
		}

		VBoxContainer layout = new()
		{
			AnchorLeft = 0,
			AnchorTop = 0,
			AnchorRight = 1,
			AnchorBottom = 1,
			OffsetLeft = 30,
			OffsetTop = 20,
			OffsetRight = -30,
			OffsetBottom = -20
		};
		layout.AddThemeConstantOverride("separation", 10);
		AddChild(layout);

		HBoxContainer header = new();
		header.AddChild(MiniGameUi.CreateLabel("QUICK TAP CHALLENGE", 30, Colors.White));
		Button closeButton = MiniGameUi.CreateButton("Close", new Vector2(120, 48));
		closeButton.Pressed += OnClosePressed;
		header.AddChild(closeButton);
		layout.AddChild(header);

		HBoxContainer status = new();
		scoreLabel = MiniGameUi.CreateLabel("Score: 0 / " + Math.Max(1, TargetScore), 22, Colors.White);
		timerLabel = MiniGameUi.CreateLabel("Time: 30", 22, Colors.White);
		status.AddChild(scoreLabel);
		status.AddChild(timerLabel);
		layout.AddChild(status);
		layout.AddChild(MiniGameUi.CreateLabel("Tap the moving target before the 30-second timer ends!", 18, Colors.White));

		playArea = new Control
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(0, 360),
			ClipContents = true
		};
		layout.AddChild(playArea);
		if (TapTargetScene == null)
		{
			GD.PrintErr("Quick Tap Challenge requires a TapTargetScene.");
			targetButton = new TapTarget();
			targetButton.Configure(TargetTexture, TargetSize);
		}
		else
		{
			targetButton = TapTargetScene.Instantiate<TapTarget>();
			targetButton.Configure(TargetTexture, TargetSize);
		}
		targetButton.Pressed += OnTargetPressed;
		playArea.AddChild(targetButton);

		countdownTimer = new Timer { WaitTime = 1, OneShot = false, ProcessMode = ProcessModeEnum.Always };
		countdownTimer.Timeout += OnCountdownTimeout;
		AddChild(countdownTimer);
		CreateResultPanel();
	}

	private void CreateResultPanel()
	{
		resultPanel = new PanelContainer
		{
			Visible = false,
			AnchorLeft = 0.5f,
			AnchorTop = 0.5f,
			AnchorRight = 0.5f,
			AnchorBottom = 0.5f,
			OffsetLeft = -230,
			OffsetTop = -150,
			OffsetRight = 230,
			OffsetBottom = 150,
			ZIndex = 10
		};
		VBoxContainer content = new();
		content.AddChild(MiniGameUi.CreateLabel("ROUND COMPLETE", 26, new Color(0.12f, 0.18f, 0.25f)));
		resultLabel = MiniGameUi.CreateLabel("", 23, new Color(0.12f, 0.18f, 0.25f));
		resultDetails = MiniGameUi.CreateLabel("", 17, new Color(0.12f, 0.18f, 0.25f));
		content.AddChild(resultLabel);
		content.AddChild(resultDetails);
		retryButton = MiniGameUi.CreateButton("Retry result", new Vector2(190, 46));
		retryButton.Hide();
		retryButton.Pressed += RetryResult;
		restartButton = MiniGameUi.CreateButton("Restart", new Vector2(190, 46));
		restartButton.Pressed += StartGame;
		Button closeButton = MiniGameUi.CreateButton("Close", new Vector2(190, 46));
		closeButton.Pressed += OnClosePressed;
		content.AddChild(retryButton);
		content.AddChild(restartButton);
		content.AddChild(closeButton);
		resultPanel.AddChild(content);
		AddChild(resultPanel);
	}

	private void StartGame()
	{
		if (isSubmitting || (gameEnded && questionId > 0 && !resultConfirmed))
			return;

		score = 0;
		timeLeft = 30;
		gameEnded = false;
		resultConfirmed = false;
		attemptWon = false;
		attemptId = Guid.NewGuid();
		targetButton.Disabled = false;
		targetNeedsPosition = true;
		resultPanel.Hide();
		UpdateStatus();
		countdownTimer.Start();
	}

	private void MoveTarget()
	{
		targetButton.Size = new Vector2(TargetSize, TargetSize);
		float maxX = Math.Max(0, playArea.Size.X - targetButton.Size.X);
		float maxY = Math.Max(0, playArea.Size.Y - targetButton.Size.Y);
		targetButton.Position = new Vector2(
			(float)GD.RandRange(0, maxX),
			(float)GD.RandRange(0, maxY));
	}

	private void OnTargetPressed()
	{
		if (gameEnded)
			return;

		score += Math.Max(1, PointsPerTap);
		UpdateStatus();
		if (score >= Math.Max(1, TargetScore))
		{
			FinishGame(true);
			return;
		}

		MoveTarget();
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
		scoreLabel.Text = "Score: " + score + " / " + Math.Max(1, TargetScore);
		timerLabel.Text = "Time: " + timeLeft;
	}

	private void FinishGame(bool won)
	{
		if (gameEnded)
			return;

		gameEnded = true;
		attemptWon = won;
		countdownTimer.Stop();
		targetButton.Disabled = true;
		resultPanel.Show();
		resultLabel.Text = won ? "Great tapping!" : "Time is up!";
		resultDetails.Text = won ? "Confirming your answer tip..." : "Confirming the coin penalty...";
		restartButton.Disabled = questionId > 0;
		if (npcId > 0 && questionId > 0)
			SubmitResult(won);
		else
		{
			resultDetails.Text = "Practice round complete. Launch from an assigned NPC to record rewards.";
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
				attemptId,
				deploymentId,
				questionId,
				gameType,
				won);
			UserSession.UpdateStudentProfile(result.TotalCoins, UserSession.Points);
			resultConfirmed = true;
			retryButton.Hide();
			restartButton.Disabled = false;
			if (won)
				resultDetails.Text = string.IsNullOrWhiteSpace(result.AnswerTip)
					? "Success! No answer tip has been added for this NPC yet."
					: "Answer tip: " + result.AnswerTip;
			else
				resultDetails.Text = result.CoinsDeducted + " coins deducted. Coins remaining: " + result.TotalCoins;
		}
		catch (Exception exception)
		{
			resultDetails.Text = "The result could not be confirmed. " + exception.Message +
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

	private void RetryResult()
	{
		SubmitResult(attemptWon);
	}

	private void OnClosePressed()
	{
		MiniGameLauncher.CloseOverlay(this);
	}
}
