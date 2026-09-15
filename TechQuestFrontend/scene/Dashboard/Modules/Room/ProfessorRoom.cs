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

	private ScrollContainer roomScrollContainer;
	private VBoxContainer roomListHost;
	private VBoxContainer roomListContainer;
	private Control roomListPanel;

	private ConfirmationDialog deleteRoomConfirmation;
	private ConfirmationDialog closeRoomConfirmation;

	private Godot.Collections.Array<Node> roomTemplate;

	private string generatedRoomCode = "";

	private int pendingDeleteRoomId = 0;
	private int pendingCloseRoomId = 0;

	public override void _Ready()
	{
		// ========================================================
		// MAIN UI
		// ========================================================

		backButton = GetNode<Button>("BackButton");

		createRoomButton = GetNode<Button>("CreateRoomButton");

		createRoomWindow = GetNode<Window>("CreateRoomWindow");

		subjectNameInput = GetNode<LineEdit>(
			"CreateRoomWindow/CreateRoomPanel/RoomCodeContainer/SubjectNameInput"
		);

		roomCodeLabel = GetNode<Label>(
			"CreateRoomWindow/CreateRoomPanel/RoomCodeContainer/RoomCodeLabel"
		);

		codeLabel = GetNode<Label>(
			"CreateRoomWindow/CreateRoomPanel/RoomCodeContainer/CodeLabel"
		);

		generateCodeButton = GetNode<Button>(
			"CreateRoomWindow/CreateRoomPanel/RoomCodeContainer/GenerateCodeButton"
		);

		saveButton = GetNode<Button>(
			"CreateRoomWindow/CreateRoomPanel/RoomCodeContainer/RoomButtonContainer/SaveButton"
		);

		cancelButton = GetNode<Button>(
			"CreateRoomWindow/CreateRoomPanel/RoomCodeContainer/RoomButtonContainer/CancelButton"
		);

		// ========================================================
		// SCROLL CONTAINER
		// ========================================================

		roomScrollContainer = GetNode<ScrollContainer>(
			"RoomManagementPanel/ScrollContainer"
		);

		roomListContainer = GetNode<VBoxContainer>(
			"RoomManagementPanel/ScrollContainer/VBoxContainer/RoomListPanel/RoomListContainer"
		);

		roomListHost = GetNode<VBoxContainer>(
			"RoomManagementPanel/ScrollContainer/VBoxContainer"
		);

		roomListPanel = GetNode<Control>(
			"RoomManagementPanel/ScrollContainer/VBoxContainer/RoomListPanel"
		);

		// ========================================================
		// CONFIRMATION DIALOGS
		// ========================================================

		deleteRoomConfirmation = GetNode<ConfirmationDialog>(
			"DeleteRoomConfirmation"
		);

		closeRoomConfirmation = GetNode<ConfirmationDialog>(
			"CloseRoomConiframation"
		);

		// ========================================================
		// ROOM TEMPLATE
		// ========================================================

		roomTemplate = roomListContainer.GetChildren();

		SetRoomTemplateVisible(false);
		roomListPanel.Visible = false;

		ConfigureScrollContainer();

		// ========================================================
		// SIGNALS
		// ========================================================

		backButton.Pressed += OnBackPressed;

		createRoomButton.Pressed += OnCreateRoomPressed;

		generateCodeButton.Pressed += OnGenerateCodePressed;

		saveButton.Pressed += OnSaveRoomPressed;

		cancelButton.Pressed += OnCancelCreateRoomPressed;

		deleteRoomConfirmation.Confirmed += OnDeleteRoomConfirmed;

		closeRoomConfirmation.Confirmed += OnCloseRoomConfirmed;

		// ========================================================
		// INITIAL STATE
		// ========================================================

		ResetCreateRoomForm();

		_ = RefreshRoomsAsync();
	}

	// ============================================================
	// SCROLL CONTAINER CONFIGURATION
	// ============================================================

	private void ConfigureScrollContainer()
	{
		if (roomScrollContainer == null ||
			roomListContainer == null)
		{
			return;
		}

		// Vertical scrolling.
		roomScrollContainer.VerticalScrollMode =
			ScrollContainer.ScrollMode.Auto;

		// Disable horizontal scrolling.
		roomScrollContainer.HorizontalScrollMode =
			ScrollContainer.ScrollMode.Disabled;

		// VBox fills available width.
		roomListContainer.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;

		// IMPORTANT:
		// Do not use ExpandFill vertically.
		// The VBox needs to be able to grow taller than
		// the visible ScrollContainer.
		roomListContainer.SizeFlagsVertical =
			Control.SizeFlags.ShrinkBegin;

		// No fixed height.
		roomListContainer.CustomMinimumSize =
			Vector2.Zero;

	}

	// ============================================================
	// BACK
	// ============================================================

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile(
			"res://scene/Dashboard/UserDashboard/Professor Dashboard/ProfessorDashboard.tscn"
		);
	}

	// ============================================================
	// CREATE ROOM
	// ============================================================

	private void OnCreateRoomPressed()
	{
		ResetCreateRoomForm();

		createRoomWindow.PopupCentered();
	}

	// ============================================================
	// GENERATE ROOM CODE
	// ============================================================

	private async void OnGenerateCodePressed()
	{
		SetCreateControlsEnabled(false);

		try
		{
			generatedRoomCode =
				await RoomApiClient.GenerateRoomCodeAsync();

			codeLabel.Text =
				generatedRoomCode;

			roomCodeLabel.Visible = true;
			codeLabel.Visible = true;
		}
		catch (Exception ex)
		{
			GD.PrintErr(
				$"Generate room code failed: {ex.Message}"
			);

			codeLabel.Text =
				"Failed to generate code";

			roomCodeLabel.Visible = true;
			codeLabel.Visible = true;
		}
		finally
		{
			SetCreateControlsEnabled(true);
		}
	}

	// ============================================================
	// SAVE / CREATE ROOM
	// ============================================================

	private async void OnSaveRoomPressed()
	{
		string roomName =
			subjectNameInput.Text.Trim();

		if (string.IsNullOrWhiteSpace(roomName))
		{
			codeLabel.Text =
				"Subject name is required";

			roomCodeLabel.Visible = true;
			codeLabel.Visible = true;

			return;
		}

		if (string.IsNullOrWhiteSpace(generatedRoomCode))
		{
			codeLabel.Text =
				"Generate room code first";

			roomCodeLabel.Visible = true;
			codeLabel.Visible = true;

			return;
		}

		SetCreateControlsEnabled(false);

		try
		{
			await RoomApiClient.CreateRoomAsync(
				roomName,
				generatedRoomCode
			);

			createRoomWindow.Hide();

			ResetCreateRoomForm();

			await RefreshRoomsAsync();
		}
		catch (Exception ex)
		{
			GD.PrintErr(
				$"Create room failed: {ex.Message}"
			);

			codeLabel.Text =
				ex.Message;

			roomCodeLabel.Visible = true;
			codeLabel.Visible = true;
		}
		finally
		{
			SetCreateControlsEnabled(true);
		}
	}

	// ============================================================
	// CANCEL CREATE ROOM
	// ============================================================

	private void OnCancelCreateRoomPressed()
	{
		createRoomWindow.Hide();

		ResetCreateRoomForm();
	}

	// ============================================================
	// LOAD ROOMS
	// ============================================================

	private async Task RefreshRoomsAsync()
	{
		try
		{
			List<RoomSummaryDto> rooms =
				await RoomApiClient.GetRoomsAsync();

			RenderRooms(rooms);
		}
		catch (Exception ex)
		{
			GD.PrintErr(
				$"Load rooms failed: {ex.Message}"
			);

			RenderMessage(
				"Unable to load rooms."
			);
		}
	}

	// ============================================================
	// RENDER ROOMS
	// ============================================================

	private void RenderRooms(
		List<RoomSummaryDto> rooms)
	{
		ClearRoomList();

		SetRoomTemplateVisible(false);
		roomListPanel.Visible = false;

		// Do NOT calculate room height manually.
		// VBoxContainer will calculate the required height
		// from its children.

		if (rooms == null || rooms.Count == 0)
		{
			RenderMessage(
				"No rooms created yet."
			);

			return;
		}

		foreach (RoomSummaryDto room in rooms)
		{
			Control row =
				CreateRoomRow(room);

			row.SizeFlagsHorizontal =
				Control.SizeFlags.ExpandFill;

			row.SizeFlagsVertical =
				Control.SizeFlags.ShrinkBegin;

			roomListHost.AddChild(row);
		}

		// Godot will finish the container layout
		// on the next idle frame.
		CallDeferred("RefreshRoomLayout");
	}

	// ============================================================
	// REFRESH ROOM LAYOUT
	// ============================================================

	private void RefreshRoomLayout()
	{
		if (!IsInstanceValid(roomListContainer))
			return;

		if (!IsInstanceValid(roomListPanel))
			return;

		if (!IsInstanceValid(roomScrollContainer))
			return;

		// Ask Godot to sort/recalculate the VBox.
		roomListHost.QueueSort();

		// Redraw the panel and scroll container.
		roomListPanel.QueueRedraw();

		roomScrollContainer.QueueRedraw();
	}

	// ============================================================
	// CREATE ROOM ROW
	// ============================================================

	private Control CreateRoomRow(
		RoomSummaryDto room)
	{
		var row = (Control)roomListPanel.Duplicate();
		row.Visible = true;
		row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

		var rowContainer = row.GetNode<VBoxContainer>("RoomListContainer");
		rowContainer.Visible = true;

		// ========================================================
		// SUBJECT
		// ========================================================

		var subjectLabel =
			rowContainer.GetNode<Label>("SubjectLabel");

		subjectLabel.Text =
			room.RoomName;
		subjectLabel.Visible = true;

		subjectLabel.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;

		// ========================================================
		// ROOM CODE
		// ========================================================

		var roomCodeLabel =
			rowContainer.GetNode<Label>("RoomCodeLabel");

		roomCodeLabel.Text =
			$"Code: {room.RoomCode}";
		roomCodeLabel.Visible = true;

		roomCodeLabel.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;

		// ========================================================
		// STUDENT COUNT
		// ========================================================

		var studentCountLabel =
			rowContainer.GetNode<Label>("StudentCountLabel");

		studentCountLabel.Text =
			$"Students: {room.StudentCount}";
		studentCountLabel.Visible = true;

		studentCountLabel.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;

		// ========================================================
		// STATUS
		// ========================================================

		var statusLabel =
			rowContainer.GetNode<Label>("StatusLabel");

		statusLabel.Text =
			$"Status: {GetDisplayStatus(room.Status)}";
		statusLabel.Visible = true;

		statusLabel.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;

		// ========================================================
		// ACTION BUTTONS
		// ========================================================

		var actionButtons =
			rowContainer.GetNode<HBoxContainer>("RoomButton");

		actionButtons.Visible = true;
		actionButtons.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;


		// ========================================================
		// MANAGE
		// ========================================================

		var manageButton =
			actionButtons.GetNode<Button>("Manage");

		manageButton.Pressed +=
			() => OnManageRoomPressed(room);

		// ========================================================
		// CLOSE
		// ========================================================

		var closeButton =
			actionButtons.GetNode<Button>("Close");

		closeButton.Disabled =
			IsClosed(room.Status);

		closeButton.Pressed +=
			() => OnCloseRoomPressed(room.RoomId);

		// ========================================================
		// DELETE
		// ========================================================

		var deleteButton =
			actionButtons.GetNode<Button>("Delete");

		deleteButton.Pressed +=
			() => OnDeleteRoomPressed(room.RoomId);

		return row;
	}

	// ============================================================
	// MANAGE ROOM
	// ============================================================

	private void OnManageRoomPressed(
		RoomSummaryDto room)
	{
		MultiplayerRoomState.SetRoom(
			room.RoomId,
			room.RoomCode,
			room.RoomName
		);

		GetTree().ChangeSceneToFile(
			"res://scene/Dashboard/Modules/Room/ProfessorWaitingRoom.tscn"
		);
	}

	// ============================================================
	// DELETE ROOM
	// ============================================================

	private void OnDeleteRoomPressed(int roomId)
	{
		pendingDeleteRoomId = roomId;

		deleteRoomConfirmation.PopupCentered();
	}

	// ============================================================
	// CLOSE ROOM
	// ============================================================

	private void OnCloseRoomPressed(int roomId)
	{
		pendingCloseRoomId = roomId;

		closeRoomConfirmation.PopupCentered();
	}

	// ============================================================
	// CONFIRM CLOSE
	// ============================================================

	private async void OnCloseRoomConfirmed()
	{
		if (pendingCloseRoomId <= 0)
			return;

		try
		{
			await RoomApiClient.CloseRoomAsync(
				pendingCloseRoomId
			);

			pendingCloseRoomId = 0;

			await RefreshRoomsAsync();
		}
		catch (Exception ex)
		{
			GD.PrintErr(
				$"Close room failed: {ex.Message}"
			);
		}
	}

	// ============================================================
	// CONFIRM DELETE
	// ============================================================

	private async void OnDeleteRoomConfirmed()
	{
		if (pendingDeleteRoomId <= 0)
			return;

		try
		{
			await RoomApiClient.DeleteRoomAsync(
				pendingDeleteRoomId
			);

			pendingDeleteRoomId = 0;

			await RefreshRoomsAsync();
		}
		catch (Exception ex)
		{
			GD.PrintErr(
				$"Delete room failed: {ex.Message}"
			);
		}
	}

	// ============================================================
	// RESET CREATE ROOM FORM
	// ============================================================

	private void ResetCreateRoomForm()
	{
		generatedRoomCode = "";

		subjectNameInput.Text = "";

		codeLabel.Text = "";

		roomCodeLabel.Visible = false;

		codeLabel.Visible = false;

		SetCreateControlsEnabled(true);
	}

	// ============================================================
	// ENABLE / DISABLE CREATE CONTROLS
	// ============================================================

	private void SetCreateControlsEnabled(
		bool enabled)
	{
		subjectNameInput.Editable =
			enabled;

		generateCodeButton.Disabled =
			!enabled;

		saveButton.Disabled =
			!enabled;

		cancelButton.Disabled =
			!enabled;
	}

	// ============================================================
	// MESSAGE
	// ============================================================

	private void RenderMessage(
		string message)
	{
		ClearRoomList();

		var messageLabel = new Label
		{
			Text = message,

			SizeFlagsHorizontal =
				Control.SizeFlags.ExpandFill,

			SizeFlagsVertical =
				Control.SizeFlags.ShrinkBegin
		};

		roomListHost.AddChild(
			messageLabel
		);

		CallDeferred(
			"RefreshRoomLayout"
		);
	}

	// ============================================================
	// CLEAR ROOM LIST
	// ============================================================

	private void ClearRoomList()
	{
		foreach (Node child in roomListHost.GetChildren())
		{
			// Keep the original hidden panel as the room template.
			if (child == roomListPanel)
				continue;

			child.QueueFree();
		}
	}

	// ============================================================
	// TEMPLATE VISIBILITY
	// ============================================================

	private void SetRoomTemplateVisible(
		bool visible)
	{
		foreach (Node templateNode in roomTemplate)
		{
			if (templateNode is CanvasItem canvasItem)
			{
				canvasItem.Visible = visible;
			}
		}
	}

	// ============================================================
	// ROOM STATUS
	// ============================================================

	private static bool IsClosed(
		string status)
	{
		return string.Equals(
			status,
			"closed",
			StringComparison.OrdinalIgnoreCase
		);
	}

	private static string GetDisplayStatus(
		string status)
	{
		return IsClosed(status)
			? "Close"
			: "Ongoing";
	}
}
