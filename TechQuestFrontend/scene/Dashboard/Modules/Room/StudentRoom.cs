using Godot;
using System;
using System.Threading.Tasks;

public partial class StudentRoom : Control
{
	private Button backButton;
	private Button joinRoomButton;
	private LineEdit codeInput;
	private AcceptDialog alertDialog;

	public override void _Ready()
	{
		backButton = GetNode<Button>("BackButton");
		joinRoomButton = GetNode<Button>("JoinRoomButton");
		codeInput = GetNode<LineEdit>("CodeInput");
		alertDialog = GetNode<AcceptDialog>("AcceptDialog");

		backButton.Pressed += OnBackPressed;
		joinRoomButton.Pressed += OnJoinRoomPressed;

	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/UserDashboard/Student Dashboard/StudentDashboard.tscn");
	}

	private async void OnJoinRoomPressed()
	{
		string roomCode = codeInput.Text.Trim().ToUpperInvariant();
		if (string.IsNullOrWhiteSpace(roomCode))
		{
			ShowError("Invalid room code");
			return;
		}

		await JoinRoomAsync(roomCode);
	}

	private async Task JoinRoomAsync(string roomCode)
	{
		SetJoinEnabled(false);

		try
		{
			RoomJoinDto room = await RoomApiClient.JoinRoomAsync(roomCode);
			MultiplayerRoomState.SetRoom(room.RoomId, room.RoomCode, room.RoomName, room.StudentId);
			GetTree().ChangeSceneToFile("res://scene/Dashboard/Modules/Room/StudentWaitingRoom.tscn");
		}
		catch (Exception ex)
		{
			GD.PrintErr($"Join room failed: {ex.Message}");
			ShowError("Invalid room code");
		}
		finally
		{
			SetJoinEnabled(true);
		}
	}

	private void SetJoinEnabled(bool enabled)
	{
		joinRoomButton.Disabled = !enabled;
		codeInput.Editable = enabled;
	}

	private void ShowError(string message)
	{
		alertDialog.DialogText = message;
		alertDialog.PopupCentered();
	}
}
