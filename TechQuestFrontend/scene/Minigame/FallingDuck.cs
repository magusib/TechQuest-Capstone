using Godot;
using System;

public partial class FallingDuck : Node2D
{
	[Export] public float PlayerSpeed { get; set; } = 360;
	[Export] public float DuckSpeed { get; set; } = 210;
	[Export] public float SpawnInterval { get; set; } = 0.9f;
	[Export] public float PlayerHalfWidth { get; set; } = 36;
	[Export] public float DuckSpawnMargin { get; set; } = 40;
	[Export] public int GameDurationSeconds { get; set; } = 30;
	[Export] public int FailureCoinPenalty { get; set; } = 5;
	[Export] public PackedScene DuckScene { get; set; }

	private CharacterBody2D player;
	private CollisionShape2D playerCollision;
	private Node2D duckContainer;
	private Label timerLabel;
	private Label statusLabel;
	private Label resultTitle;
	private Label resultMessage;
	private Label answerTipLabel;
	private Label coinPenaltyLabel;
	private Button restartButton;
	private Button retryButton;
	private Control touchControls;
	private Sprite2D background;
	private Control resultOverlay;
	private Timer countdownTimer;
	private float spawnTime;
	private bool moveLeft;
	private bool moveRight;
	private int npcId;
	private int deploymentId;
	private int questionId;
	private string gameType = "FallingDuck";
	private int timeLeft;
	private int timerSeconds = 30;
	private bool gameEnded;
	private bool resultConfirmed;
	private bool isSubmitting;
	private bool attemptWon;
	private Guid attemptId;
	private Vector2 playerStartPosition;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		player = GetNode<CharacterBody2D>("PlayArea/Player");
		playerCollision = GetNode<CollisionShape2D>("PlayArea/Player/PlayerCollision");
		duckContainer = GetNode<Node2D>("PlayArea/DuckContainer");
		timerLabel = GetNode<Label>("HUD/MainUI/HeaderPanel/HeaderLayout/TimerLabel");
		statusLabel = GetNode<Label>("HUD/MainUI/BottomPanel/StatusLabel");
		resultOverlay = GetNode<Control>("HUD/MainUI/ResultOverlay");
		resultTitle = GetNode<Label>("HUD/MainUI/ResultOverlay/ResultPanel/ResultLayout/ResultTitle");
		resultMessage = GetNode<Label>("HUD/MainUI/ResultOverlay/ResultPanel/ResultLayout/ResultMessage");
		answerTipLabel = GetNode<Label>("HUD/MainUI/ResultOverlay/ResultPanel/ResultLayout/AnswerTipLabel");
		coinPenaltyLabel = GetNode<Label>("HUD/MainUI/ResultOverlay/ResultPanel/ResultLayout/CoinPenaltyLabel");
		restartButton = GetNode<Button>("HUD/MainUI/ResultOverlay/ResultPanel/ResultLayout/PlayAgainButton");
		retryButton = GetNode<Button>("HUD/MainUI/ResultOverlay/ResultPanel/ResultLayout/RetryResultButton");
		touchControls = GetNodeOrNull<Control>("HUD/MainUI/TouchControls");
		background = GetNodeOrNull<Sprite2D>("Background");
		countdownTimer = GetNode<Timer>("CountdownTimer");

		bool isAndroid = OS.GetName() == "Android";
		if (touchControls != null)
			touchControls.Visible = isAndroid;
		if (background != null)
		{
			Vector2 viewportSize = GetViewportRect().Size;
			background.Scale = new Vector2(
				Mathf.Max(1f, viewportSize.X / 1280f),
				Mathf.Max(1f, viewportSize.Y / 720f));
		}

		Button closeButton = GetNode<Button>("HUD/MainUI/HeaderPanel/HeaderLayout/CloseButton");
		Button proceedButton = GetNode<Button>("HUD/MainUI/ResultOverlay/ResultPanel/ResultLayout/ResultCloseButton");
		closeButton.Pressed += OnClosePressed;
		restartButton.Pressed += StartGame;
		proceedButton.Pressed += OnClosePressed;
		if (questionId > 0)
		{
			closeButton.Hide();
			restartButton.Hide();
			proceedButton.Hide();
			proceedButton.Text = "Proceed to Question";
		}
		retryButton.Pressed += RetryResult;
		GetNode<Button>("HUD/MainUI/TouchControls/MoveLeftButton").ButtonDown += OnLeftButtonDown;
		GetNode<Button>("HUD/MainUI/TouchControls/MoveLeftButton").ButtonUp += OnLeftButtonUp;
		GetNode<Button>("HUD/MainUI/TouchControls/MoveRightButton").ButtonDown += OnRightButtonDown;
		GetNode<Button>("HUD/MainUI/TouchControls/MoveRightButton").ButtonUp += OnRightButtonUp;
		countdownTimer.Timeout += OnCountdownTimeout;
		countdownTimer.ProcessMode = ProcessModeEnum.Always;
		playerStartPosition = player.Position;
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
		if (player != null)
			player.Velocity = Vector2.Zero;
		ClearDucks();
	}

	public override void _Process(double delta)
	{
		if (gameEnded)
			return;

		spawnTime += (float)delta;
		if (spawnTime >= Math.Max(0.2f, SpawnInterval))
		{
			spawnTime = 0;
			SpawnDuck();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (gameEnded || player == null)
			return;

		float direction = 0;
		if (Input.IsActionPressed("move_left") || moveLeft)
			direction -= 1;
		if (Input.IsActionPressed("move_right") || moveRight)
			direction += 1;

		player.Velocity = new Vector2(direction * PlayerSpeed, 0);
		player.MoveAndSlide();
		float halfWidth = GetPlayerHalfWidth();
		float x = Mathf.Clamp(player.Position.X, halfWidth, Math.Max(halfWidth, GetViewportRect().Size.X - halfWidth));
		player.Position = new Vector2(x, player.Position.Y);
		CheckDuckCollisions();
	}

	private void CheckDuckCollisions()
	{
		if (duckContainer == null)
			return;

		foreach (Node child in duckContainer.GetChildren())
		{
			if (child is FallingDuckTarget duck
				&& (duck.GetOverlappingBodies().Contains(player) || IsPlayerTouchingDuck(duck)))
			{
				FinishGame(false);
				return;
			}
		}
	}

	private bool IsPlayerTouchingDuck(FallingDuckTarget duck)
	{
		CollisionShape2D duckCollision = duck.GetNodeOrNull<CollisionShape2D>("DuckCollision");
		if (playerCollision?.Shape is not RectangleShape2D playerShape
			|| duckCollision?.Shape is not CircleShape2D duckShape)
			return false;

		Transform2D playerTransform = playerCollision.GlobalTransform;
		Vector2 halfSize = playerShape.Size / 2;
		Vector2 halfExtents = new(
			Mathf.Abs(playerTransform.X.X) * halfSize.X + Mathf.Abs(playerTransform.Y.X) * halfSize.Y,
			Mathf.Abs(playerTransform.X.Y) * halfSize.X + Mathf.Abs(playerTransform.Y.Y) * halfSize.Y);
		Transform2D duckTransform = duckCollision.GlobalTransform;
		float duckRadius = duckShape.Radius * Math.Max(duckTransform.X.Length(), duckTransform.Y.Length());
		Vector2 difference = duckCollision.GlobalPosition - playerCollision.GlobalPosition;
		Vector2 nearestPoint = new(
			Mathf.Clamp(difference.X, -halfExtents.X, halfExtents.X),
			Mathf.Clamp(difference.Y, -halfExtents.Y, halfExtents.Y));

		return (difference - nearestPoint).LengthSquared() <= duckRadius * duckRadius;
	}

	private float GetPlayerHalfWidth()
	{
		if (playerCollision?.Shape is RectangleShape2D rectangle)
			return rectangle.Size.X * Mathf.Abs(playerCollision.Scale.X) / 2;
		if (playerCollision?.Shape is CircleShape2D circle)
			return circle.Radius * Mathf.Abs(playerCollision.Scale.X);
		if (playerCollision?.Shape is CapsuleShape2D capsule)
			return capsule.Radius * Mathf.Abs(playerCollision.Scale.X);
		return Math.Max(1, PlayerHalfWidth);
	}

	private void StartGame()
	{
		if (isSubmitting || (gameEnded && questionId > 0 && !resultConfirmed))
			return;

		if (DuckScene == null)
		{
			gameEnded = true;
			statusLabel.Text = "Assign the reusable FallingDuckTarget scene in the Inspector.";
			return;
		}

		ClearDucks();
		player.Position = playerStartPosition;
		player.Velocity = Vector2.Zero;
		spawnTime = 0;
		moveLeft = false;
		moveRight = false;
		timeLeft = questionId > 0 ? timerSeconds : Math.Max(1, GameDurationSeconds);
		gameEnded = false;
		resultConfirmed = false;
		attemptWon = false;
		if (questionId == 0)
			attemptId = Guid.NewGuid();
		statusLabel.Text = "Avoid the falling ducks!";
		timerLabel.Text = timeLeft.ToString();
		resultOverlay.Hide();
		retryButton.Hide();
		countdownTimer.Start();
	}

	private void SpawnDuck()
	{
		if (DuckScene == null)
		{
			GD.PrintErr("Falling Duck requires the reusable DuckScene.");
			return;
		}

		FallingDuckTarget duck = DuckScene.Instantiate<FallingDuckTarget>();
		duck.FallingSpeed = DuckSpeed;
		duck.BodyEntered += OnDuckBodyEntered;
		float screenWidth = GetViewportRect().Size.X;
		float margin = Math.Max(1, DuckSpawnMargin);
		duck.Position = new Vector2((float)GD.RandRange(margin, Math.Max(margin, screenWidth - margin)), 0);
		duckContainer.AddChild(duck);
	}

	private void OnDuckBodyEntered(Node2D body)
	{
		if (!gameEnded && body == player)
			FinishGame(false);
	}

	private void ClearDucks()
	{
		if (duckContainer == null)
			return;

		foreach (Node child in duckContainer.GetChildren())
		{
			if (child is FallingDuckTarget duck)
			{
				duck.Monitoring = false;
				duck.SetPhysicsProcess(false);
				duck.QueueFree();
			}
		}
	}

	private void OnCountdownTimeout()
	{
		if (gameEnded)
			return;

		timeLeft--;
		timerLabel.Text = timeLeft.ToString();
		if (timeLeft <= 0)
			FinishGame(false);
	}

	private void FinishGame(bool won)
	{
		if (gameEnded)
			return;

		gameEnded = true;
		attemptWon = won;
		countdownTimer.Stop();
		player.Velocity = Vector2.Zero;
		ClearDucks();
		resultOverlay.Show();
		resultTitle.Text = won ? "You survived!" : "A duck caught you!";
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
				Button proceedButton = GetNode<Button>("HUD/MainUI/ResultOverlay/ResultPanel/ResultLayout/ResultCloseButton");
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
	private void OnLeftButtonDown() => moveLeft = true;
	private void OnLeftButtonUp() => moveLeft = false;
	private void OnRightButtonDown() => moveRight = true;
	private void OnRightButtonUp() => moveRight = false;
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
