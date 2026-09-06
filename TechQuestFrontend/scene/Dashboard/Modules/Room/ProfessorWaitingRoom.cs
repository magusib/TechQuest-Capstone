using Godot;

public partial class ProfessorWaitingRoom : Control
{
	private Label subjectLabel;
	private Label roomCode;
	private Label studentCountLabel;
	private Button backButton;

	public override void _Ready()
	{
		subjectLabel = GetNode<Label>("Header/SubjectLabel");
		roomCode = GetNode<Label>("Header/RoomCodePanel/RoomCode");
		studentCountLabel = GetNode<Label>("Header/StudentCountLabel");
		backButton = GetNode<Button>("Header/Button");

		subjectLabel.Text = $"Subject: {MultiplayerRoomState.RoomName}";
		roomCode.Text = MultiplayerRoomState.RoomCode;
		studentCountLabel.Text = "Student Joined: 0";

		backButton.Pressed += OnBackPressed;
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/Modules/Room/ProfessorRoom.tscn");
	}
}
