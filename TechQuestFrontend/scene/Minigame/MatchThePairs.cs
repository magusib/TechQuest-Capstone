using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class MatchThePairs : Control
{
	[Export] public Godot.Collections.Array<Texture2D> CharacterImages { get; set; } = new();
	[Export] public Texture2D CardBackTexture { get; set; }
	[Export] public Vector2 CardSize { get; set; } = new(138, 155);
	[Export] public int GridColumns { get; set; } = 4;
	[Export] public int NumberOfPairs { get; set; } = 4;
	[Export] public float MismatchDelay { get; set; } = 0.8f;
	[Export] public int PointsPerMatchedPair { get; set; } = 10;
	[Export] public int GameDurationSeconds { get; set; } = 15;
	[Export] public int FailureCoinPenalty { get; set; } = 5;
	[Export] public PackedScene MemoryCardScene { get; set; }

	private GridContainer cardGrid;
	private Label scoreLabel;
	private Label timerLabel;
	private Label matchedPairsLabel;
	private Label totalPairsLabel;
	private Label statusLabel;
	private Label resultTitle;
	private Label resultMessage;
	private Label answerTipLabel;
	private Label coinPenaltyLabel;
	private Button restartButton;
	private Button retryButton;
	private Control resultOverlay;
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
	private int timerSeconds = 15;
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
		cardGrid = GetNode<GridContainer>("MainPanel/MainMargin/MainLayout/CardArea/CardGrid");
		scoreLabel = GetNode<Label>("MainPanel/MainMargin/MainLayout/Header/ScoreContainer/ScoreLabel");
		timerLabel = GetNode<Label>("MainPanel/MainMargin/MainLayout/Header/TimerContainer/TimerLabel");
		matchedPairsLabel = GetNode<Label>("MainPanel/MainMargin/MainLayout/ProgressContainer/MatchedPairsLabel");
		totalPairsLabel = GetNode<Label>("MainPanel/MainMargin/MainLayout/ProgressContainer/TotalPairsLabel");
		statusLabel = GetNode<Label>("MainPanel/MainMargin/MainLayout/BottomPanel/BottomLayout/StatusLabel");
		restartButton = GetNode<Button>("MainPanel/MainMargin/MainLayout/BottomPanel/BottomLayout/RestartButton");
		resultOverlay = GetNode<Control>("ResultOverlay");
		resultTitle = GetNode<Label>("ResultOverlay/ResultPanel/ResultLayout/ResultTitle");
		resultMessage = GetNode<Label>("ResultOverlay/ResultPanel/ResultLayout/ResultMessage");
		answerTipLabel = GetNode<Label>("ResultOverlay/ResultPanel/ResultLayout/AnswerTipLabel");
		coinPenaltyLabel = GetNode<Label>("ResultOverlay/ResultPanel/ResultLayout/CoinPenaltyLabel");
		retryButton = GetNode<Button>("ResultOverlay/ResultPanel/ResultLayout/RetryResultButton");
		countdownTimer = GetNode<Timer>("CountdownTimer");
		mismatchTimer = GetNode<Timer>("MismatchTimer");

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
		mismatchTimer.Timeout += OnMismatchTimeout;
		countdownTimer.ProcessMode = ProcessModeEnum.Always;
		mismatchTimer.ProcessMode = ProcessModeEnum.Always;
		mismatchTimer.WaitTime = Math.Max(0.1f, MismatchDelay);
		if (questionId == 0)
			StartGame();
	}

	public void Configure(int assignedDeploymentId, int assignedNpcId, int assignedQuestionId, string assignedGameType, int assignedTimerSeconds, Guid assignedAttemptId)
	{
		deploymentId = assignedDeploymentId;
		npcId = assignedNpcId;
		questionId = assignedQuestionId;
		gameType = assignedGameType;
		timerSeconds = 15;
		attemptId = assignedAttemptId;
	}

	public void StartAssignedGame() => StartGame();

	public void StopGame()
	{
		gameEnded = true;
		countdownTimer?.Stop();
		mismatchTimer?.Stop();
	}

	private void StartGame()
	{
		if (isSubmitting || (gameEnded && questionId > 0 && !resultConfirmed))
			return;

		ClearCards();
		selectedCards.Clear();
		score = 0;
		matchedPairs = 0;
		timeLeft = questionId > 0 ? timerSeconds : Math.Max(1, GameDurationSeconds);
		gameEnded = false;
		resultConfirmed = false;
		attemptWon = false;
		if (questionId == 0)
			attemptId = Guid.NewGuid();
		resultOverlay.Hide();
		retryButton.Hide();
		CreateDeck();
		UpdateStatus();
		if (MemoryCardScene == null)
		{
			gameEnded = true;
			statusLabel.Text = "Assign the reusable MemoryCard scene in the Inspector.";
			return;
		}
		if (deck.Count == 0)
		{
			gameEnded = true;
			statusLabel.Text = "Add at least one character image and pair in the Inspector.";
			return;
		}
		BuildCards();
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
			(deck[index], deck[swapIndex]) = (deck[swapIndex], deck[index]);
		}
	}

	private void BuildCards()
	{
		if (MemoryCardScene == null)
			return;

		cardGrid.Columns = Math.Max(1, GridColumns);
		for (int index = 0; index < deck.Count; index++)
		{
			MemoryCard card = MemoryCardScene.Instantiate<MemoryCard>();
			card.CustomMinimumSize = CardSize;
			if (CardBackTexture != null)
				card.CardBackTexture = CardBackTexture;
			card.Selected += OnCardSelected;
			cardGrid.AddChild(card);
			card.Setup(index, deck[index]);
			cards.Add(card);
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

		mismatchTimer.WaitTime = Math.Max(0.1f, MismatchDelay);
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
		scoreLabel.Text = score.ToString();
		matchedPairsLabel.Text = "Matched pairs: " + matchedPairs;
		totalPairsLabel.Text = "Total pairs: " + (deck.Count / 2);
		timerLabel.Text = timeLeft.ToString();
	}

	private void ClearCards()
	{
		foreach (Node child in cardGrid.GetChildren())
		{
			if (child is Control control)
				control.Hide();
			if (child is MemoryCard card)
				card.SetCardDisabled(true);
			child.QueueFree();
		}
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

		resultOverlay.Show();
		resultTitle.Text = won ? "You found every pair!" : "Time is up!";
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
			resultConfirmed = true;
			retryButton.Hide();
			UserSession.UpdateStudentProfile(result.TotalCoins, result.TotalPoints);
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
