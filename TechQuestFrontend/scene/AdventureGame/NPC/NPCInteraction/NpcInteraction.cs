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
	private AcceptDialog lifespanDialog;

	public override void _Ready()
	{
		interactionPopup = GetNode<Control>("NPCInteractionPopUp");
		promptLabel = interactionPopup.GetNode<Label>("Label");
		promptLabel.AutowrapMode = TextServer.AutowrapMode.Word;
		yesButton = interactionPopup.GetNode<Button>("YesButton");
		yesButton.Pressed += AcceptChallenge;
		interactionPopup.GetNode<Button>("NoButton").Pressed += DeclineChallenge;
		lifespanDialog = new AcceptDialog
		{
			Title = "Lifespan Required",
			DialogText = "You have no remaining Lifespan. Restore it through recovery or purchase before challenging an NPC."
		};
		AddChild(lifespanDialog);
		Hide();
	}

	public async void ShowChallenge(Node source, int assignedDeploymentId)
	{
		if (UserSession.Lifespan <= 0)
		{
			lifespanDialog.PopupCentered();
			return;
		}

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
				"Subject: " + configuration.SubjectName;

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
		if (isLoading || interactionSource == null ||
			!GodotObject.IsInstanceValid(interactionSource) ||
			!interactionSource.IsInsideTree())
			return;

		Node source = interactionSource;
		int currentDeploymentId = deploymentId;
		requestGeneration++;
		Hide();
		interactionSource = null;
		isLoading = false;
		MiniGameLauncher.OpenCharacterChallenge(source, currentDeploymentId);
	}
}
