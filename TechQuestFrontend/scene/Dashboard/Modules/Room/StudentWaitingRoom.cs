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

	public override void _Ready()
	{
		subjectLabel = GetNode<Label>("Header/SubjectLabel");
		roomCode = GetNode<Label>("Header/RoomCodePanel/RoomCode");
		studentCountLabel = GetNode<Label>("Header/StudentCountLabel");
		backButton = GetNode<Button>("Header/Button");
		startButton = GetNode<Button>("StartButton");
		changeAvatarButton = GetNode<Button>("ChangeAvatarButton");
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

	private void OnStartPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/Modules/Room/StudentQuizSession.tscn");
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
