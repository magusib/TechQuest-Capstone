using Godot;

public partial class StudentWaitingRoom : Control
{
	private Label subjectLabel;
	private Label roomCode;
	private Label studentCountLabel;
	private Button backButton;
	private Button startButton;

	public override void _Ready()
	{
		subjectLabel = GetNode<Label>("Header/SubjectLabel");
		roomCode = GetNode<Label>("Header/RoomCodePanel/RoomCode");
		studentCountLabel = GetNode<Label>("Header/StudentCountLabel");
		backButton = GetNode<Button>("Header/Button");
		startButton = GetNode<Button>("StartButton");

		subjectLabel.Text = $"Subject: {MultiplayerRoomState.RoomName}";
		roomCode.Text = MultiplayerRoomState.RoomCode;
		studentCountLabel.Text = "Student: Waiting";
		startButton.Disabled = true;

		backButton.Pressed += OnBackPressed;
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/Modules/Room/StudentRoom.tscn");
	}
}
