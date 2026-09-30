using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class ProfessorRoomResult : Control
{
	private Label subjectLabel;
	private Label roomCodeLabel;
	private Panel studentTemplate;
	private VBoxContainer resultsContainer;
	private AcceptDialog alertDialog;

	public override void _Ready()
	{
		resultsContainer = GetNode<VBoxContainer>("RoomResultPanel/ScrollContainer/RoomResultContainer");
		subjectLabel = GetNode<Label>("RoomResultPanel/ScrollContainer/RoomResultContainer/SubjectNameLabel");
		roomCodeLabel = GetNode<Label>("RoomResultPanel/ScrollContainer/RoomResultContainer/RoomCodeLabel");
		studentTemplate = GetNode<Panel>("RoomResultPanel/ScrollContainer/RoomResultContainer/StudentScorePanel");
		studentTemplate.Visible = false;
		alertDialog = new AcceptDialog { Title = "Student results" };
		AddChild(alertDialog);
		GetNode<Button>("BackButton").Pressed += OnBackPressed;
		subjectLabel.Text = $"Subject: {MultiplayerRoomState.RoomName}";
		roomCodeLabel.Text = $"Room code: {MultiplayerRoomState.RoomCode}";
		_ = LoadResultsAsync();
	}

	private async Task LoadResultsAsync()
	{
		try
		{
			List<RoomResultSummaryDto> results = await RoomApiClient.GetResultsAsync(MultiplayerRoomState.RoomId);
			foreach (RoomResultSummaryDto result in results)
			{
				Panel row = (Panel)studentTemplate.Duplicate();
				row.Visible = true;
				resultsContainer.AddChild(row);
				VBoxContainer rowContent = row.GetNode<VBoxContainer>("StudentScoreContainer");
				rowContent.GetNode<Label>("StudentNameLabel").Text = $"Student: {result.StudentName}";
				rowContent.GetNode<Label>("StudentScoreLabel").Text = $"Score: {result.Score}/{result.TotalQuestions}";
			}
			if (results.Count == 0)
				alertDialog.DialogText = "No students have completed the quiz yet.";
			else
				return;
		}
		catch (Exception exception)
		{
			alertDialog.DialogText = exception.Message;
		}
		alertDialog.PopupCentered();
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/Modules/Room/ProfessorWaitingRoom.tscn");
	}
}
