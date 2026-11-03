using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class HigherLower : Control
{
	private Label playerNameLabel;
	private Label playerScoreLabel;
	private Label playerPredictionLabel;
	private Label playerResultLabel;
	private Label npcNameLabel;
	private Label npcScoreLabel;
	private Label npcPredictionLabel;
	private Label npcResultLabel;
	private Label currentNumberLabel;
	private Label nextNumberLabel;
	private Label actualNumberResultLabel;
	private Button higherButton;
	private Button lowerButton;
	private Panel gameResultPopup;
	private Label resultLabel;
	private Label finalPlayerScoreLabel;
	private Label finalNpcScoreLabel;
	private Label coinsDeductedLabel;
	private Label pointsDeductedLabel;
	private Button returnButton;

	private CharacterChallengeStartDto challenge;
	private Node mapSource;
	private readonly HashSet<int> usedNumbers = new();
	private readonly Random random = new();
	private int currentNumber;
	private int playerScore;
	private int npcScore;
	private string playerPrediction = "";
	private bool roundLocked;
	private bool challengeEnded;
	private bool resultSubmitted;
	private bool isReady;
	private string pendingOutcome = "";

	public override void _Ready()
	{
		playerNameLabel = GetNodeOrNull<Label>("MainContainer/ContestantsRow/PlayerPanel/PlayerInfoContainer/PlayerNameLabel");
		playerScoreLabel = GetNodeOrNull<Label>("MainContainer/ContestantsRow/PlayerPanel/PlayerInfoContainer/PlayerScoreLabel");
		playerPredictionLabel = GetNodeOrNull<Label>("MainContainer/ContestantsRow/PlayerPanel/PlayerInfoContainer/PlayerPredictionLabel");
		playerResultLabel = GetNodeOrNull<Label>("MainContainer/ContestantsRow/PlayerPanel/PlayerInfoContainer/PlayerResultLabel");
		npcNameLabel = GetNodeOrNull<Label>("MainContainer/ContestantsRow/NPCPanel/NPCInfoContainer/NPCNameLabel");
		npcScoreLabel = GetNodeOrNull<Label>("MainContainer/ContestantsRow/NPCPanel/NPCInfoContainer/NPCScoreLabel");
		npcPredictionLabel = GetNodeOrNull<Label>("MainContainer/ContestantsRow/NPCPanel/NPCInfoContainer/NPCPredictionLabel");
		npcResultLabel = GetNodeOrNull<Label>("MainContainer/ContestantsRow/NPCPanel/NPCInfoContainer/NPCResultLabel");
		currentNumberLabel = GetNodeOrNull<Label>("MainContainer/ContestantsRow/CenterGamePanel/GameContainer/NumberCardsContainer/CurrentNumberCard/CurrentNumberLabel");
		nextNumberLabel = GetNodeOrNull<Label>("MainContainer/ContestantsRow/CenterGamePanel/GameContainer/NumberCardsContainer/NextNumberCard/NextNumberLabel");
		actualNumberResultLabel = GetNodeOrNull<Label>("MainContainer/ContestantsRow/CenterGamePanel/GameContainer/ActualNumberResultLabel");
		higherButton = GetNodeOrNull<Button>("MainContainer/PredictionButtonsContainer/HigherButton");
		lowerButton = GetNodeOrNull<Button>("MainContainer/PredictionButtonsContainer/LowerButton");
		gameResultPopup = GetNodeOrNull<Panel>("GameResultPopup");
		resultLabel = GetNodeOrNull<Label>("GameResultPopup/ResultContainer/ResultLabel");
		finalPlayerScoreLabel = GetNodeOrNull<Label>("GameResultPopup/ResultContainer/FinalPlayerScoreLabel");
		finalNpcScoreLabel = GetNodeOrNull<Label>("GameResultPopup/ResultContainer/FinalNPCScoreLabel");
		coinsDeductedLabel = GetNodeOrNull<Label>("GameResultPopup/ResultContainer/CoinsDeductedLabel");
		pointsDeductedLabel = GetNodeOrNull<Label>("GameResultPopup/ResultContainer/PointsDeductedLabel");
		returnButton = GetNodeOrNull<Button>("GameResultPopup/ResultContainer/ReturnButton");

		if (higherButton != null)
			higherButton.Pressed += () => SubmitPrediction("HIGHER");
		if (lowerButton != null)
			lowerButton.Pressed += () => SubmitPrediction("LOWER");
		if (returnButton != null)
			returnButton.Pressed += ReturnToMap;

		if (gameResultPopup != null)
			gameResultPopup.Visible = false;
		isReady = true;
		if (challenge != null)
			CallDeferred(nameof(BeginChallenge));
	}

	public void Configure(CharacterChallengeStartDto assignedChallenge, Node source)
	{
		challenge = assignedChallenge;
		mapSource = source;
		if (isReady)
			CallDeferred(nameof(BeginChallenge));
	}

	private void BeginChallenge()
	{
		if (challenge == null || !isReady)
			return;

		if (playerNameLabel != null)
			playerNameLabel.Text = "PLAYER: " + challenge.CharacterName;
		if (npcNameLabel != null)
			npcNameLabel.Text = "NPC: " + challenge.NpcName;
		currentNumber = GenerateUnusedNumber();
		UpdateScoreLabels();
		BeginRound();
	}

	private void BeginRound()
	{
		if (challengeEnded)
			return;

		playerPrediction = "";
		roundLocked = false;
		if (currentNumberLabel != null)
			currentNumberLabel.Text = currentNumber.ToString();
		if (nextNumberLabel != null)
			nextNumberLabel.Text = "?";
		if (playerPredictionLabel != null)
			playerPredictionLabel.Text = "Prediction: -";
		if (npcPredictionLabel != null)
			npcPredictionLabel.Text = "Prediction: -";
		if (playerResultLabel != null)
			playerResultLabel.Text = "Result: -";
		if (npcResultLabel != null)
			npcResultLabel.Text = "Result: -";
		if (actualNumberResultLabel != null)
			actualNumberResultLabel.Text = "Choose HIGHER or LOWER";
		SetPredictionButtons(true);
	}

	private void SubmitPrediction(string prediction)
	{
		if (roundLocked || challengeEnded || string.IsNullOrWhiteSpace(prediction))
			return;

		playerPrediction = prediction;
		roundLocked = true;
		SetPredictionButtons(false);
		string npcPrediction = random.Next(0, 2) == 0 ? "HIGHER" : "LOWER";
		int nextNumber = GenerateUnusedNumberDifferentFrom(currentNumber);
		bool playerCorrect = IsCorrectPrediction(playerPrediction, currentNumber, nextNumber);
		bool npcCorrect = IsCorrectPrediction(npcPrediction, currentNumber, nextNumber);
		if (playerCorrect)
			playerScore++;
		if (npcCorrect)
			npcScore++;

		if (playerPredictionLabel != null)
			playerPredictionLabel.Text = "Prediction: " + playerPrediction;
		if (npcPredictionLabel != null)
			npcPredictionLabel.Text = "Prediction: " + npcPrediction;
		UpdateScoreLabels();
		RevealRoundAsync(nextNumber, playerCorrect, npcCorrect);
	}

	private async void RevealRoundAsync(int nextNumber, bool playerCorrect, bool npcCorrect)
	{
		await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
		if (nextNumberLabel != null)
			nextNumberLabel.Text = nextNumber.ToString();
		if (playerResultLabel != null)
			playerResultLabel.Text = "Result: " + (playerCorrect ? "CORRECT" : "INCORRECT");
		if (npcResultLabel != null)
			npcResultLabel.Text = "Result: " + (npcCorrect ? "CORRECT" : "INCORRECT");
		if (actualNumberResultLabel != null)
			actualNumberResultLabel.Text = "Actual number: " + nextNumber;

		if (playerScore != npcScore)
		{
			challengeEnded = true;
			await FinishChallengeAsync(playerScore > npcScore ? "WIN" : "LOSE");
			return;
		}

		currentNumber = nextNumber;
		await ToSignal(GetTree().CreateTimer(0.7f), SceneTreeTimer.SignalName.Timeout);
		BeginRound();
	}

	private async Task FinishChallengeAsync(string outcome)
	{
		if (resultSubmitted || challenge == null)
			return;

		pendingOutcome = outcome;
		resultSubmitted = true;
		try
		{
			CharacterChallengeResultDto result = await MiniGameApiClient.SubmitCharacterChallengeAsync(
				challenge.AttemptId, challenge.DeploymentId, playerScore, npcScore, outcome);
			UserSession.UpdateStudentProfile(result.TotalCoins, result.TotalPoints);
			ShowResult(result);
		}
		catch (Exception exception)
		{
			resultSubmitted = false;
			challengeEnded = false;
			if (returnButton != null)
				returnButton.Text = "RETRY SAVE";
			ShowError(exception.Message);
		}
	}

	private void ShowResult(CharacterChallengeResultDto result)
	{
		if (gameResultPopup == null)
			return;
		if (resultLabel != null)
			resultLabel.Text = result.Outcome == "WIN" ? "WIN" : "LOSE";
		if (finalPlayerScoreLabel != null)
			finalPlayerScoreLabel.Text = "Player Score: " + result.PlayerScore;
		if (finalNpcScoreLabel != null)
			finalNpcScoreLabel.Text = "NPC Score: " + result.NpcScore;
		if (coinsDeductedLabel != null)
			coinsDeductedLabel.Text = "Coins Deducted: " + result.CoinsDeducted;
		if (pointsDeductedLabel != null)
			pointsDeductedLabel.Text = "Points Deducted: " + result.PointsDeducted;
		if (returnButton != null)
			returnButton.Text = "RETURN";
		gameResultPopup.Visible = true;
	}

	private void ReturnToMap()
	{
		if (!resultSubmitted && !string.IsNullOrWhiteSpace(pendingOutcome))
		{
			challengeEnded = true;
			_ = FinishChallengeAsync(pendingOutcome);
			return;
		}

		if (!resultSubmitted)
			return;
		SceneTree tree = GetTree();
		tree.Paused = false;
		Node overlay = GetParent();
		if (overlay != null)
			overlay.QueueFree();
	}

	private int GenerateUnusedNumber()
	{
		int number;
		do
		{
			number = random.Next(1, 101);
		} while (usedNumbers.Contains(number));
		usedNumbers.Add(number);
		return number;
	}

	private int GenerateUnusedNumberDifferentFrom(int previous)
	{
		int number;
		do
		{
			number = GenerateUnusedNumber();
		} while (number == previous);
		return number;
	}

	private static bool IsCorrectPrediction(string prediction, int current, int next)
	{
		return prediction == "HIGHER" ? next > current : next < current;
	}

	private void SetPredictionButtons(bool enabled)
	{
		if (higherButton != null)
			higherButton.Disabled = !enabled;
		if (lowerButton != null)
			lowerButton.Disabled = !enabled;
	}

	private void UpdateScoreLabels()
	{
		if (playerScoreLabel != null)
			playerScoreLabel.Text = "Score: " + playerScore;
		if (npcScoreLabel != null)
			npcScoreLabel.Text = "Score: " + npcScore;
	}

	private void ShowError(string message)
	{
		GD.PrintErr("HigherLower result could not be saved: " + message);
		if (actualNumberResultLabel != null)
			actualNumberResultLabel.Text = "Could not save result. Try again.";
	}
}
