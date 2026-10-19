using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class MatchThePairs : Control
{
	[Export]
	public Godot.Collections.Array<Texture2D> CharacterImages { get; set; } = new();

	[Export]
	public Texture2D CardBackTexture { get; set; }

	[Export]
	public Texture2D BackgroundTexture { get; set; }

	[Export]
	public Color BackgroundColor { get; set; } = new Color(0.37f, 0.76f, 0.83f);

	[Export]
	public Vector2 CardSize { get; set; } = new Vector2(138, 155);

	[Export]
	public int CardSpacing { get; set; } = 14;

	[Export]
	public int GridColumns { get; set; } = 4;

	[Export]
	public int NumberOfPairs { get; set; } = 4;

	[Export]
	public float MismatchDelay { get; set; } = 0.8f;

	[Export]
	public int PointsPerMatchedPair { get; set; } = 10;

	[Export]
	public PackedScene MemoryCardScene { get; set; }

	private GridContainer cardGrid;
	private Label scoreLabel;
	private Label timerLabel;
	private Label pairLabel;
	private Label resultLabel;
	private Label resultDetails;
	private Button restartButton;
	private Button retryButton;
	private PanelContainer resultPanel;
	private Timer countdownTimer;
	private Timer mismatchTimer;
	private readonly List<MemoryCard> cards = new();
	private readonly List<MemoryCard> selectedCards = new();
	private readonly List<Texture2D> deck = new();
	private int questionId;
	private int npcId;
	private int deploymentId;
	private string gameType = "MatchThePairs";
	private int timeLeft;
	private int score;
	private int matchedPairs;
	private bool gameEnded;
	private bool resultConfirmed;
	private bool isSubmitting;
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
		mismatchTimer.Stop();
	}

	private void BuildInterface()
	{
		if (BackgroundTexture != null)
		{
			TextureRect background = new()
			{
				Texture = BackgroundTexture,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale
			};
			MiniGameUi.Fill(background);
			background.MouseFilter = MouseFilterEnum.Ignore;
			AddChild(background);
		}
		else
		{
			ColorRect background = new() { Color = BackgroundColor, MouseFilter = MouseFilterEnum.Ignore };
			MiniGameUi.Fill(background);
			AddChild(background);
		}

		VBoxContainer layout = new()
		{
			AnchorLeft = 0,
			AnchorTop = 0,
			AnchorRight = 1,
			AnchorBottom = 1,
			OffsetLeft = 28,
			OffsetTop = 18,
			OffsetRight = -28,
			OffsetBottom = -18,
			Theme = new Theme()
		};
		layout.AddThemeConstantOverride("separation", 10);
		AddChild(layout);

		HBoxContainer header = new();
		header.AddChild(MiniGameUi.CreateLabel("MATCH THE PAIRS", 30, Colors.White));
		Button closeButton = MiniGameUi.CreateButton("Close", new Vector2(120, 48));
		closeButton.Pressed += OnClosePressed;
		header.AddChild(closeButton);
		layout.AddChild(header);

		HBoxContainer status = new();
		scoreLabel = MiniGameUi.CreateLabel("Score: 0", 22, Colors.White);
		pairLabel = MiniGameUi.CreateLabel("Pairs: 0 / 0", 22, Colors.White);
		timerLabel = MiniGameUi.CreateLabel("Time: 30", 22, Colors.White);
		status.AddChild(scoreLabel);
		status.AddChild(pairLabel);
		status.AddChild(timerLabel);
		layout.AddChild(status);
		layout.AddChild(MiniGameUi.CreateLabel(
			"Flip two cards at a time. Find every matching pair before time runs out.",
			18,
			Colors.White));

		cardGrid = new GridContainer
		{
			Columns = Math.Max(2, GridColumns),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		cardGrid.AddThemeConstantOverride("h_separation", Math.Max(0, CardSpacing));
		cardGrid.AddThemeConstantOverride("v_separation", Math.Max(0, CardSpacing));
		layout.AddChild(cardGrid);

		countdownTimer = new Timer { WaitTime = 1, OneShot = false, ProcessMode = ProcessModeEnum.Always };
		countdownTimer.Timeout += OnCountdownTimeout;
		AddChild(countdownTimer);
		mismatchTimer = new Timer
		{
			WaitTime = Math.Max(0.1f, MismatchDelay),
			OneShot = true,
			ProcessMode = ProcessModeEnum.Always
		};
		mismatchTimer.Timeout += OnMismatchTimeout;
		AddChild(mismatchTimer);

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
		retryButton.Visible = false;
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

		ClearCards();
		selectedCards.Clear();
		score = 0;
		matchedPairs = 0;
		timeLeft = 30;
		gameEnded = false;
		resultConfirmed = false;
		attemptWon = false;
		attemptId = Guid.NewGuid();
		resultPanel.Hide();
		CreateDeck();
		BuildCards();
		UpdateStatus();
		if (deck.Count == 0 || MemoryCardScene == null)
		{
			gameEnded = true;
			countdownTimer.Stop();
			return;
		}
		countdownTimer.Start();
	}

	private void CreateDeck()
	{
		deck.Clear();
		int availablePairs = Math.Min(Math.Max(0, NumberOfPairs), CharacterImages.Count);
		for (int index = 0; index < availablePairs; index++)
		{
			Texture2D texture = CharacterImages[index];
			if (texture != null)
			{
				deck.Add(texture);
				deck.Add(texture);
			}
		}

		for (int index = deck.Count - 1; index > 0; index--)
		{
			int swapIndex = GD.RandRange(0, index);
			Texture2D texture = deck[index];
			deck[index] = deck[swapIndex];
			deck[swapIndex] = texture;
		}
	}

	private void BuildCards()
	{
		if (MemoryCardScene == null)
		{
			GD.PrintErr("Match the Pairs requires a MemoryCardScene.");
			resultLabel.Text = "Card scene is missing.";
			resultDetails.Text = "Set the MemoryCardScene property in the Inspector.";
			resultPanel.Show();
			return;
		}

		for (int index = 0; index < deck.Count; index++)
		{
			MemoryCard card = MemoryCardScene.Instantiate<MemoryCard>();
			card.CustomMinimumSize = CardSize;
			card.CardBackTexture = CardBackTexture;
			card.Selected += OnCardSelected;
			cardGrid.AddChild(card);
			card.Setup(index, deck[index]);
			cards.Add(card);
		}

		if (deck.Count == 0)
		{
			GD.PrintErr("Match the Pairs requires at least one valid character image.");
			resultLabel.Text = "No character images are configured.";
			resultDetails.Text = "Add images and choose at least one pair in the Inspector.";
			resultPanel.Show();
		}
	}

	private void OnCardSelected(MemoryCard card)
	{
		if (gameEnded || mismatchTimer.TimeLeft > 0 || selectedCards.Count >= 2 || card == null)
			return;

		card.SetCardDisabled(true);
		card.ShowFace();
		selectedCards.Add(card);
		if (selectedCards.Count == 2)
			CheckSelectedCards();
	}

	private void CheckSelectedCards()
	{
		if (selectedCards[0].FaceTexture == selectedCards[1].FaceTexture)
		{
			selectedCards[0].SetMatched(true);
			selectedCards[1].SetMatched(true);
			selectedCards.Clear();
			matchedPairs++;
			score += Math.Max(0, PointsPerMatchedPair);
			UpdateStatus();
			if (matchedPairs == deck.Count / 2)
				FinishGame(true);
			return;
		}

		mismatchTimer.Start();
	}

	private void OnMismatchTimeout()
	{
		if (gameEnded)
			return;

		foreach (MemoryCard card in selectedCards)
		{
			card.ShowBack();
			card.SetCardDisabled(false);
		}
		selectedCards.Clear();
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
		scoreLabel.Text = "Score: " + score;
		pairLabel.Text = "Pairs: " + matchedPairs + " / " + (deck.Count / 2);
		timerLabel.Text = "Time: " + timeLeft;
	}

	private void ClearCards()
	{
		foreach (Node child in cardGrid.GetChildren())
			child.QueueFree();
		cards.Clear();
	}

	private void FinishGame(bool won)
	{
		if (gameEnded)
			return;

		gameEnded = true;
		attemptWon = won;
		countdownTimer.Stop();
		mismatchTimer.Stop();
		foreach (MemoryCard card in cards)
			card.SetCardDisabled(true);

		resultPanel.Show();
		resultLabel.Text = won ? "You found every pair!" : "Time is up!";
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
