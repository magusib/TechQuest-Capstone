using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class ProfessorRoom : Control
{
	private Button backButton;
	private Button createRoomButton;
	private Window createRoomWindow;
	private LineEdit subjectNameInput;
	private Label roomCodeLabel;
	private Label codeLabel;
	private Button generateCodeButton;
	private Button saveButton;
	private Button cancelButton;
	private VBoxContainer roomListContainer;
	private ConfirmationDialog deleteRoomConfirmation;

	private string generatedRoomCode = "";
	private int pendingDeleteRoomId;

	public override void _Ready()
	{
		backButton = GetNode<Button>("BackButton");
		createRoomButton = GetNode<Button>("CreateRoomButton");
		createRoomWindow = GetNode<Window>("CreateRoomWindow");
		subjectNameInput = GetNode<LineEdit>("CreateRoomWindow/CreateRoomPanel/RoomCodeContainer/SubjectNameInput");
		roomCodeLabel = GetNode<Label>("CreateRoomWindow/CreateRoomPanel/RoomCodeContainer/RoomCodeLabel");
		codeLabel = GetNode<Label>("CreateRoomWindow/CreateRoomPanel/RoomCodeContainer/CodeLabel");
		generateCodeButton = GetNode<Button>("CreateRoomWindow/CreateRoomPanel/RoomCodeContainer/GenerateCodeButton");
		saveButton = GetNode<Button>("CreateRoomWindow/CreateRoomPanel/RoomCodeContainer/RoomButtonContainer/SaveButton");
		cancelButton = GetNode<Button>("CreateRoomWindow/CreateRoomPanel/RoomCodeContainer/RoomButtonContainer/CancelButton");
		roomListContainer = GetNode<VBoxContainer>("RoomManagementPanel/ScrollContainer/VBoxContainer/RoomListPanel/RoomListContainer");
		deleteRoomConfirmation = GetNode<ConfirmationDialog>("DeleteRoomConfirmation");

		backButton.Pressed += OnBackPressed;
		createRoomButton.Pressed += OnCreateRoomPressed;
		generateCodeButton.Pressed += OnGenerateCodePressed;
		saveButton.Pressed += OnSaveRoomPressed;
		cancelButton.Pressed += OnCancelCreateRoomPressed;
		deleteRoomConfirmation.Confirmed += OnDeleteRoomConfirmed;

		ResetCreateRoomForm();
		_ = RefreshRoomsAsync();
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/UserDashboard/Professor Dashboard/ProfessorDashboard.tscn");
	}

	private void OnCreateRoomPressed()
	{
		ResetCreateRoomForm();
		createRoomWindow.PopupCentered();
	}

	private async void OnGenerateCodePressed()
	{
		SetCreateControlsEnabled(false);

		try
		{
			generatedRoomCode = await RoomApiClient.GenerateRoomCodeAsync();
			codeLabel.Text = generatedRoomCode;
			roomCodeLabel.Visible = true;
			codeLabel.Visible = true;
		}
		catch (Exception ex)
		{
			GD.PrintErr($"Generate room code failed: {ex.Message}");
			codeLabel.Text = "Failed to generate code";
			roomCodeLabel.Visible = true;
			codeLabel.Visible = true;
		}
		finally
		{
			SetCreateControlsEnabled(true);
		}
	}

	private async void OnSaveRoomPressed()
	{
		string roomName = subjectNameInput.Text.Trim();
		if (string.IsNullOrWhiteSpace(roomName))
		{
			codeLabel.Text = "Subject name is required";
			roomCodeLabel.Visible = true;
			codeLabel.Visible = true;
			return;
		}

		if (string.IsNullOrWhiteSpace(generatedRoomCode))
		{
			codeLabel.Text = "Generate room code first";
			roomCodeLabel.Visible = true;
			codeLabel.Visible = true;
			return;
		}

		SetCreateControlsEnabled(false);

		try
		{
			await RoomApiClient.CreateRoomAsync(roomName, generatedRoomCode);
			createRoomWindow.Hide();
			ResetCreateRoomForm();
			await RefreshRoomsAsync();
		}
		catch (Exception ex)
		{
			GD.PrintErr($"Create room failed: {ex.Message}");
			codeLabel.Text = ex.Message;
			roomCodeLabel.Visible = true;
			codeLabel.Visible = true;
		}
		finally
		{
			SetCreateControlsEnabled(true);
		}
	}

	private void OnCancelCreateRoomPressed()
	{
		createRoomWindow.Hide();
		ResetCreateRoomForm();
	}

	private async Task RefreshRoomsAsync()
	{
		try
		{
			List<RoomSummaryDto> rooms = await RoomApiClient.GetRoomsAsync();
			RenderRooms(rooms);
		}
		catch (Exception ex)
		{
			GD.PrintErr($"Load rooms failed: {ex.Message}");
			RenderMessage("Unable to load rooms.");
		}
	}

	private void RenderRooms(List<RoomSummaryDto> rooms)
	{
		ClearRoomList();

		if (rooms.Count == 0)
		{
			RenderMessage("No rooms created yet.");
			return;
		}

		foreach (RoomSummaryDto room in rooms)
			roomListContainer.AddChild(CreateRoomRow(room));
	}

	private Control CreateRoomRow(RoomSummaryDto room)
	{
		var panel = new Panel
		{
			CustomMinimumSize = new Vector2(0, 110),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};

		var row = new HBoxContainer
		{
			AnchorRight = 1,
			AnchorBottom = 1,
			OffsetLeft = 16,
			OffsetTop = 14,
			OffsetRight = -16,
			OffsetBottom = -14,
			Alignment = BoxContainer.AlignmentMode.Center
		};
		row.AddThemeConstantOverride("separation", 30);
		panel.AddChild(row);

		row.AddChild(CreateLabel(room.RoomName, 220));
		row.AddChild(CreateLabel($"Code: {room.RoomCode}", 170));
		row.AddChild(CreateLabel($"Students: {room.StudentCount}", 150));
		row.AddChild(CreateLabel($"Status: {room.Status}", 150));

		var manageButton = new Button { Text = "Manage", CustomMinimumSize = new Vector2(110, 40) };
		manageButton.Pressed += () => OnManageRoomPressed(room);
		row.AddChild(manageButton);

		var deleteButton = new Button { Text = "Delete", CustomMinimumSize = new Vector2(110, 40) };
		deleteButton.Pressed += () => OnDeleteRoomPressed(room.RoomId);
		row.AddChild(deleteButton);

		return panel;
	}

	private static Label CreateLabel(string text, float width)
	{
		return new Label
		{
			Text = text,
			CustomMinimumSize = new Vector2(width, 40),
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
	}

	private void OnManageRoomPressed(RoomSummaryDto room)
	{
		MultiplayerRoomState.SetRoom(room.RoomId, room.RoomCode, room.RoomName);
		GetTree().ChangeSceneToFile("res://scene/Dashboard/Modules/Room/ProfessorWaitingRoom.tscn");
	}

	private void OnDeleteRoomPressed(int roomId)
	{
		pendingDeleteRoomId = roomId;
		deleteRoomConfirmation.PopupCentered();
	}

	private async void OnDeleteRoomConfirmed()
	{
		if (pendingDeleteRoomId <= 0)
			return;

		try
		{
			await RoomApiClient.DeleteRoomAsync(pendingDeleteRoomId);
			pendingDeleteRoomId = 0;
			await RefreshRoomsAsync();
		}
		catch (Exception ex)
		{
			GD.PrintErr($"Delete room failed: {ex.Message}");
		}
	}

	private void ResetCreateRoomForm()
	{
		generatedRoomCode = "";
		subjectNameInput.Text = "";
		codeLabel.Text = "";
		roomCodeLabel.Visible = false;
		codeLabel.Visible = false;
		SetCreateControlsEnabled(true);
	}

	private void SetCreateControlsEnabled(bool enabled)
	{
		subjectNameInput.Editable = enabled;
		generateCodeButton.Disabled = !enabled;
		saveButton.Disabled = !enabled;
		cancelButton.Disabled = !enabled;
	}

	private void RenderMessage(string message)
	{
		ClearRoomList();
		roomListContainer.AddChild(CreateLabel(message, 500));
	}

	private void ClearRoomList()
	{
		foreach (Node child in roomListContainer.GetChildren())
			child.QueueFree();
	}
}
