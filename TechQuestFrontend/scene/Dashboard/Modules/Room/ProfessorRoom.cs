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
	private Panel roomListPanel;
	private ConfirmationDialog deleteRoomConfirmation;
	private ConfirmationDialog closeRoomConfirmation;
	private Godot.Collections.Array<Node> roomTemplate;

	private string generatedRoomCode = "";
	private int pendingDeleteRoomId;
	private int pendingCloseRoomId;

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
		roomListPanel = GetNode<Panel>("RoomManagementPanel/ScrollContainer/VBoxContainer/RoomListPanel");
		deleteRoomConfirmation = GetNode<ConfirmationDialog>("DeleteRoomConfirmation");
		closeRoomConfirmation = GetNode<ConfirmationDialog>("CloseRoomConiframation");
		roomTemplate = roomListContainer.GetChildren();
		SetRoomTemplateVisible(false);

		backButton.Pressed += OnBackPressed;
		createRoomButton.Pressed += OnCreateRoomPressed;
		generateCodeButton.Pressed += OnGenerateCodePressed;
		saveButton.Pressed += OnSaveRoomPressed;
		cancelButton.Pressed += OnCancelCreateRoomPressed;
		deleteRoomConfirmation.Confirmed += OnDeleteRoomConfirmed;
		closeRoomConfirmation.Confirmed += OnCloseRoomConfirmed;

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
		SetRoomTemplateVisible(false);

		float contentHeight = Math.Max(350, rooms.Count * 220 + 30);
		roomListPanel.CustomMinimumSize = new Vector2(0, contentHeight);
		roomListContainer.CustomMinimumSize = new Vector2(0, contentHeight - 26);

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
		var row = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		row.AddThemeConstantOverride("separation", 45);

		var subjectLabel = (Label)roomTemplate[0].Duplicate();
		subjectLabel.Text = room.RoomName;
		subjectLabel.Visible = true;
		row.AddChild(subjectLabel);

		var roomCodeLabel = (Label)roomTemplate[1].Duplicate();
		roomCodeLabel.Text = $"Code: {room.RoomCode}";
		roomCodeLabel.Visible = true;
		row.AddChild(roomCodeLabel);

		var studentCountLabel = (Label)roomTemplate[2].Duplicate();
		studentCountLabel.Text = $"Students: {room.StudentCount}";
		studentCountLabel.Visible = true;
		row.AddChild(studentCountLabel);

		var statusLabel = (Label)roomTemplate[3].Duplicate();
		statusLabel.Text = $"Status: {GetDisplayStatus(room.Status)}";
		statusLabel.Visible = true;
		row.AddChild(statusLabel);

		var actionButtons = (HBoxContainer)roomTemplate[4].Duplicate();
		actionButtons.Visible = true;
		row.AddChild(actionButtons);

		var manageButton = actionButtons.GetNode<Button>("Manage");
		manageButton.Pressed += () => OnManageRoomPressed(room);

		var closeButton = actionButtons.GetNode<Button>("Close");
		closeButton.Disabled = IsClosed(room.Status);
		closeButton.Pressed += () => OnCloseRoomPressed(room.RoomId);

		var deleteButton = actionButtons.GetNode<Button>("Delete");
		deleteButton.Pressed += () => OnDeleteRoomPressed(room.RoomId);

		return row;
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

	private void OnCloseRoomPressed(int roomId)
	{
		pendingCloseRoomId = roomId;
		closeRoomConfirmation.PopupCentered();
	}

	private async void OnCloseRoomConfirmed()
	{
		if (pendingCloseRoomId <= 0)
			return;

		try
		{
			await RoomApiClient.CloseRoomAsync(pendingCloseRoomId);
			pendingCloseRoomId = 0;
			await RefreshRoomsAsync();
		}
		catch (Exception ex)
		{
			GD.PrintErr($"Close room failed: {ex.Message}");
		}
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
		var messageLabel = new Label
		{
			Text = message,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		roomListContainer.AddChild(messageLabel);
	}

	private void ClearRoomList()
	{
		foreach (Node child in roomListContainer.GetChildren())
		{
			if (!roomTemplate.Contains(child))
				child.Free();
		}
	}

	private void SetRoomTemplateVisible(bool visible)
	{
		foreach (Node templateNode in roomTemplate)
			((CanvasItem)templateNode).Visible = visible;
	}

	private static bool IsClosed(string status)
	{
		return string.Equals(status, "closed", StringComparison.OrdinalIgnoreCase);
	}

	private static string GetDisplayStatus(string status)
	{
		return IsClosed(status) ? "Close" : "Ongoing";
	}
}
