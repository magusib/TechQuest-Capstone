using Godot;
using System;

public partial class NpcInteraction : Control
{
	private Control interactionPopup;
	private Label promptLabel;
	private Button yesButton;
	private Node interactionSource;
	private int deploymentId;
	private int requestGeneration;
	private bool isLoading;
	private string challengeState = "";
	private string answerTip = "";

	public override void _Ready()
	{
		interactionPopup = GetNode<Control>("NPCInteractionPopUp");
		promptLabel = interactionPopup.GetNode<Label>("Label");
		promptLabel.AutowrapMode = TextServer.AutowrapMode.Word;
		yesButton = interactionPopup.GetNode<Button>("YesButton");
		yesButton.Pressed += AcceptChallenge;
		interactionPopup.GetNode<Button>("NoButton").Pressed += DeclineChallenge;
		Hide();
	}

	public async void ShowChallenge(Node source, int assignedDeploymentId)
	{
		int currentGeneration = ++requestGeneration;
		interactionSource = source;
		deploymentId = assignedDeploymentId;
		isLoading = true;
		yesButton.Disabled = true;
		promptLabel.Text = "Do you want to accept this Subject Challenge?\nLoading NPC configuration...";
		Show();
		try
		{
			StudentNpcConfigurationDto configuration =
				await MiniGameApiClient.GetStudentConfigurationAsync(deploymentId);
			if (!GodotObject.IsInstanceValid(this) ||
				!IsInsideTree() ||
				currentGeneration != requestGeneration ||
				deploymentId != configuration.DeploymentId)
				return;

			challengeState = configuration.ChallengeState;
			answerTip = configuration.AnswerTip;
			promptLabel.Text =
				"Do you want to accept this Subject Challenge?\n" +
				"Subject: " + configuration.SubjectName + "\n" +
				"Mini-game: " + configuration.MiniGameName + "\n" +
				"Question time limit: " + configuration.TimerSeconds + " seconds";
			if (configuration.ChallengeState == "QuestionReady")
				promptLabel.Text += "\nYour mini-game result is saved. Continue to the answer tip and question.";
			else if (configuration.ChallengeState == "MiniGamePending")
				promptLabel.Text += "\nThis challenge is in progress. Continue the assigned mini-game.";
			else if (configuration.AlreadyStarted)
				promptLabel.Text += "\nThis challenge has already been started and cannot be restarted.";
			else if (!configuration.MiniGameId.HasValue)
				promptLabel.Text += "\nThis NPC does not have a mini-game assigned.";

			yesButton.Text = configuration.AlreadyStarted ? "Continue" : "Yes";
			yesButton.Disabled = (configuration.AlreadyStarted &&
				configuration.ChallengeState != "QuestionReady" &&
				configuration.ChallengeState != "MiniGamePending") ||
				(!configuration.AlreadyStarted && !configuration.MiniGameId.HasValue) ||
				string.IsNullOrWhiteSpace(configuration.SubjectName);
		}
		catch (Exception exception)
		{
			if (currentGeneration != requestGeneration)
				return;

			GD.PrintErr("NPC challenge configuration could not be loaded: " + exception.Message);
			promptLabel.Text = "NPC challenge configuration could not be loaded.\n" + exception.Message;
		}
		finally
		{
			if (currentGeneration == requestGeneration)
				isLoading = false;
		}
	}

	private void AcceptChallenge()
	{
		if (isLoading || yesButton.Disabled)
			return;

		Hide();
		if (interactionSource == null || !GodotObject.IsInstanceValid(interactionSource) ||
			!interactionSource.IsInsideTree())
		{
			GD.PrintErr("NPC challenge could not be accepted because its source is no longer available.");
			return;
		}

		if (challengeState == "QuestionReady")
			MiniGameLauncher.ResumeQuestionFromMap(interactionSource, deploymentId, answerTip);
		else
			MiniGameLauncher.OpenForDeployment(interactionSource, deploymentId);
		interactionSource = null;
	}

	private void DeclineChallenge()
	{
		requestGeneration++;
		Hide();
		interactionSource = null;
		isLoading = false;
	}
}
