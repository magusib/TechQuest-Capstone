using Godot;
using System;

public partial class FallingDuck : Control
{
	[Export]
	public Texture2D PlayerTexture { get; set; }

	[Export]
	public Texture2D DuckTexture { get; set; }

	[Export]
	public Texture2D BackgroundTexture { get; set; }

	[Export]
	public Color BackgroundColor { get; set; } = new Color(0.45f, 0.74f, 0.91f);

	[Export]
	public Vector2 PlayerSize { get; set; } = new Vector2(72, 104);

	[Export]
	public float PlayerSpeed { get; set; } = 360;

	[Export]
	public Vector2 DuckSize { get; set; } = new Vector2(70, 54);

	[Export]
	public float DuckSpeed { get; set; } = 210;

	[Export]
	public float SpawnInterval { get; set; } = 0.9f;

	[Export]
	public PackedScene DuckScene { get; set; }

	private Node2D world;
	private CharacterBody2D player;
	private CollisionShape2D playerCollision;
	private Label timerLabel;
	private Label statusLabel;
	private Label resultLabel;
	private Label resultDetails;
	private Button restartButton;
	private Button retryButton;
	private PanelContainer resultPanel;
	private Timer countdownTimer;
	private float spawnTime;
	private bool moveLeft;
	private bool moveRight;
	private int npcId;
	private int deploymentId;
	private int questionId;
	private string gameType = "FallingDuck";
	private int timeLeft;
	private bool gameEnded;
	private bool resultConfirmed;
	private bool isSubmitting;
	private bool attemptWon;
	private Guid attemptId;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		BuildInterface();
		BuildPlayer();
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
		float halfWidth = PlayerSize.X / 2;
		float x = Mathf.Clamp(player.Position.X, halfWidth, GetViewportRect().Size.X - halfWidth);
		player.Position = new Vector2(x, player.Position.Y);
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

		world = new Node2D { Name = "World" };
		AddChild(world);

		VBoxContainer layout = new()
		{
			AnchorLeft = 0,
			AnchorTop = 0,
			AnchorRight = 1,
			AnchorBottom = 1,
			OffsetLeft = 24,
			OffsetTop = 12,
			OffsetRight = -24,
			OffsetBottom = -12
		};
		layout.AddThemeConstantOverride("separation", 8);
		AddChild(layout);

		HBoxContainer header = new();
		header.AddChild(MiniGameUi.CreateLabel("FALLING DUCK", 30, Colors.White));
		Button closeButton = MiniGameUi.CreateButton("Close", new Vector2(120, 48));
		closeButton.Pressed += OnClosePressed;
		header.AddChild(closeButton);
		layout.AddChild(header);

		HBoxContainer status = new();
		timerLabel = MiniGameUi.CreateLabel("Time: 30", 22, Colors.White);
		statusLabel = MiniGameUi.CreateLabel("Avoid the ducks!", 22, Colors.White);
		status.AddChild(timerLabel);
		status.AddChild(statusLabel);
		layout.AddChild(status);

		Control playSpace = new()
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore
		};
		layout.AddChild(playSpace);

		HBoxContainer mobileControls = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		Button leftButton = MiniGameUi.CreateButton("LEFT", new Vector2(150, 52));
		leftButton.ButtonDown += OnLeftButtonDown;
		leftButton.ButtonUp += OnLeftButtonUp;
		Button rightButton = MiniGameUi.CreateButton("RIGHT", new Vector2(150, 52));
		rightButton.ButtonDown += OnRightButtonDown;
		rightButton.ButtonUp += OnRightButtonUp;
		mobileControls.AddChild(leftButton);
		mobileControls.AddChild(rightButton);
		layout.AddChild(mobileControls);

		countdownTimer = new Timer { WaitTime = 1, OneShot = false, ProcessMode = ProcessModeEnum.Always };
		countdownTimer.Timeout += OnCountdownTimeout;
		AddChild(countdownTimer);
		CreateResultPanel();
	}

	private void BuildPlayer()
	{
		player = new CharacterBody2D
		{
			Name = "Player",
			CollisionLayer = 1,
			CollisionMask = 0,
			Position = new Vector2(GetViewportRect().Size.X / 2, GetViewportRect().Size.Y - 120)
		};
		Sprite2D sprite = new() { Texture = PlayerTexture, Centered = true };
		if (PlayerTexture != null)
		{
			sprite.Scale = new Vector2(
				PlayerSize.X / Math.Max(1, PlayerTexture.GetWidth()),
				PlayerSize.Y / Math.Max(1, PlayerTexture.GetHeight()));
		}
		else
		{
			Polygon2D placeholder = new()
			{
				Polygon = new Vector2[]
				{
					new Vector2(-30, -44),
					new Vector2(30, -44),
					new Vector2(32, 40),
					new Vector2(-32, 40)
				},
				Color = new Color(0.25f, 0.4f, 0.95f)
			};
			player.AddChild(placeholder);
		}
		player.AddChild(sprite);

		RectangleShape2D shape = new()
		{
			Size = PlayerSize
		};
		playerCollision = new CollisionShape2D { Shape = shape };
		player.AddChild(playerCollision);
		world.AddChild(player);
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

		ClearDucks();
		player.Position = new Vector2(GetViewportRect().Size.X / 2, GetViewportRect().Size.Y - 120);
		player.Velocity = Vector2.Zero;
		spawnTime = 0;
		moveLeft = false;
		moveRight = false;
		timeLeft = 30;
		gameEnded = false;
		resultConfirmed = false;
		attemptWon = false;
		attemptId = Guid.NewGuid();
		statusLabel.Text = "Avoid the ducks!";
		timerLabel.Text = "Time: " + timeLeft;
		resultPanel.Hide();
		countdownTimer.Start();
	}

	private void SpawnDuck()
	{
		FallingDuckTarget duck;
		if (DuckScene == null)
		{
			duck = new FallingDuckTarget();
			duck.AddChild(new Sprite2D { Name = "Sprite2D" });
			duck.AddChild(new CollisionShape2D { Name = "CollisionShape2D" });
			GD.PrintErr("Falling Duck is using a generated duck because DuckScene is missing.");
		}
		else
		{
			duck = DuckScene.Instantiate<FallingDuckTarget>();
		}

		duck.Configure(DuckTexture, DuckSpeed, DuckSize);
		float screenWidth = GetViewportRect().Size.X;
		float halfDuckWidth = DuckSize.X / 2;
		duck.Position = new Vector2(
			(float)GD.RandRange(halfDuckWidth, Math.Max(halfDuckWidth, screenWidth - halfDuckWidth)),
			110);
		duck.BodyEntered += OnDuckBodyEntered;
		world.AddChild(duck);
	}

	private void OnDuckBodyEntered(Node2D body)
	{
		if (!gameEnded && body == player)
			FinishGame(false);
	}

	private void ClearDucks()
	{
		foreach (Node child in world.GetChildren())
		{
			if (child is FallingDuckTarget duck)
			{
				duck.SetProcess(false);
				duck.QueueFree();
			}
		}
	}

	private void OnCountdownTimeout()
	{
		if (gameEnded)
			return;

		timeLeft--;
		timerLabel.Text = "Time: " + timeLeft;
		if (timeLeft <= 0)
			FinishGame(true);
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
		resultPanel.Show();
		resultLabel.Text = won ? "You survived!" : "A duck caught you!";
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

	private void OnLeftButtonDown()
	{
		moveLeft = true;
	}

	private void OnLeftButtonUp()
	{
		moveLeft = false;
	}

	private void OnRightButtonDown()
	{
		moveRight = true;
	}

	private void OnRightButtonUp()
	{
		moveRight = false;
	}

	private void OnClosePressed()
	{
		MiniGameLauncher.CloseOverlay(this);
	}
}
