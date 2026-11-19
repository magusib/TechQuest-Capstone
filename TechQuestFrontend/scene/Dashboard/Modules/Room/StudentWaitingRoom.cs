using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class StudentWaitingRoom : Control
{
	private Label subjectLabel;
	private Label roomCode;
	private Label studentCountLabel;
	private Button backButton;
	private Button startButton;
	private Button changeAvatarButton;
	private GridContainer avatarGrid;
	private Control avatarTemplate;
	private float refreshElapsed;
	private bool refreshing;
	private bool starting;
	private AcceptDialog attemptLimitPopup;

	public override void _Ready()
	{
		if (!FeatureAccessGuard.Require(this, FeatureAccessGuard.Room, FeatureAccessGuard.DashboardMessage))
			return;

		subjectLabel = GetNode<Label>("Header/SubjectLabel");
		roomCode = GetNode<Label>("Header/RoomCodePanel/RoomCode");
		studentCountLabel = GetNode<Label>("Header/StudentCountLabel");
		backButton = GetNode<Button>("Header/Button");
		startButton = GetNode<Button>("StartButton");
		changeAvatarButton = GetNode<Button>("ChangeAvatarButton");
		attemptLimitPopup = GetNode<AcceptDialog>("QuizAttemptLimitPopup");
		avatarGrid = GetNode<GridContainer>("AvatarPanel/StudentScrool/AvatarGridContianer");
		avatarTemplate = avatarGrid.GetChild<Control>(0);
		avatarTemplate.Visible = false;

		subjectLabel.Text = $"Subject: {MultiplayerRoomState.RoomName}";
		roomCode.Text = MultiplayerRoomState.RoomCode;
		studentCountLabel.Text = "Students joined: 0";

		backButton.Pressed += OnBackPressed;
		startButton.Pressed += OnStartPressed;
		changeAvatarButton.Pressed += OnChangeAvatarPressed;
		_ = RefreshParticipantsAsync();
	}

	public override void _Process(double delta)
	{
		refreshElapsed += (float)delta;
		if (refreshElapsed >= 3)
		{
			refreshElapsed = 0;
			_ = RefreshParticipantsAsync();
		}
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/Modules/Room/StudentRoom.tscn");
	}

	private async void OnStartPressed()
	{
		if (starting)
			return;

		starting = true;
		startButton.Disabled = true;
		try
		{
			RoomQuizAttemptDto attempt = await RoomApiClient.StartAttemptAsync(
				MultiplayerRoomState.RoomId, Guid.NewGuid());
			MultiplayerRoomState.AttemptId = attempt.AttemptId;
			GetTree().ChangeSceneToFile("res://scene/Dashboard/Modules/Room/StudentQuizSession.tscn");
		}
		catch (RoomApiException exception) when (exception.ErrorCode == "quiz_attempt_limit_reached")
		{
			attemptLimitPopup.PopupCentered();
		}
		catch (Exception exception)
		{
			GD.PrintErr($"Start room failed: {exception.Message}");
		}
		finally
		{
			starting = false;
			startButton.Disabled = false;
		}
	}

	private void OnChangeAvatarPressed()
	{
		MultiplayerRoomState.ReturnToWaitingRoom = true;
		GetTree().ChangeSceneToFile("res://scene/Dashboard/Modules/Character/StudentAvatar.tscn");
	}

	private async Task RefreshParticipantsAsync()
	{
		if (refreshing)
			return;

		refreshing = true;
		try
		{
			List<RoomParticipantDto> participants = await RoomApiClient.GetParticipantsAsync(MultiplayerRoomState.RoomId);
			studentCountLabel.Text = $"Students joined: {participants.Count}";
			foreach (Node child in avatarGrid.GetChildren())
			{
				if (child != avatarTemplate)
					child.QueueFree();
			}

			foreach (RoomParticipantDto participant in participants)
			{
				Control card = (Control)avatarTemplate.Duplicate();
				card.Visible = true;
				avatarGrid.AddChild(card);
				TextureRect avatar = card.GetNode<TextureRect>("AvatarContainer/AvatarTexture");
				Label name = card.GetNode<Label>("AvatarContainer/StudentNameLabel");
				name.Text = participant.Name;
				if (!string.IsNullOrWhiteSpace(participant.Avatar))
					avatar.Texture = await AccountApiClient.LoadAvatarAsync(participant.Avatar);
			}
		}
		catch (Exception exception)
		{
			GD.PrintErr($"Load room participants failed: {exception.Message}");
		}
		finally
		{
			refreshing = false;
		}
	}
}
