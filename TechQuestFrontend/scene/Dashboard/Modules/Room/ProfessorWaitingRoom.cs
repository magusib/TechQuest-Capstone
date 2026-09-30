using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

public partial class ProfessorWaitingRoom : Control
{
	private Label subjectLabel;
	private Label roomCode;
	private Label studentCountLabel;
	private Button backButton;
	private Button uploadQuizButton;
	private Button downloadFormatButton;
	private Button resultsButton;
	private Button uploadFileButton;
	private Button uploadButton;
	private Button cancelButton;
	private Label fileLabel;
	private LineEdit timerInput;
	private Window uploadPopup;
	private GridContainer avatarGrid;
	private Control avatarTemplate;
	private FileDialog importDialog;
	private FileDialog exportDialog;
	private string selectedQuizPath = "";
	private float refreshElapsed;
	private bool refreshing;

	public override void _Ready()
	{
		subjectLabel = GetNode<Label>("Header/SubjectLabel");
		roomCode = GetNode<Label>("Header/RoomCodePanel/RoomCode");
		studentCountLabel = GetNode<Label>("Header/StudentCountLabel");
		backButton = GetNode<Button>("Header/Button");
		uploadQuizButton = GetNode<Button>("UplaodQuizButton");
		downloadFormatButton = GetNode<Button>("DownloadFormatButton");
		resultsButton = GetNode<Button>("ResultsButton");
		uploadPopup = GetNode<Window>("UploadQuizPopup");
		fileLabel = GetNode<Label>("UploadQuizPopup/UploadQuizContainer/FileLabel");
		timerInput = GetNode<LineEdit>("UploadQuizPopup/UploadQuizContainer/TimerInput");
		uploadFileButton = GetNode<Button>("UploadQuizPopup/UploadQuizContainer/UploadFileButton");
		uploadButton = GetNode<Button>("UploadQuizPopup/UploadQuizContainer/ButtonContainer/UploadButton");
		cancelButton = GetNode<Button>("UploadQuizPopup/UploadQuizContainer/ButtonContainer/CancelButton");
		avatarGrid = GetNode<GridContainer>("AvatarPanel/StudentScrool/AvatarGridContianer");
		avatarTemplate = avatarGrid.GetChild<Control>(0);
		avatarTemplate.Visible = false;

		importDialog = new FileDialog
		{
			FileMode = FileDialog.FileModeEnum.OpenFile,
			Access = FileDialog.AccessEnum.Filesystem,
			Filters = new string[] { "*.csv ; CSV quiz" },
			Title = "Select quiz CSV"
		};
		exportDialog = new FileDialog
		{
			FileMode = FileDialog.FileModeEnum.SaveFile,
			Access = FileDialog.AccessEnum.Filesystem,
			Filters = new string[] { "*.csv ; CSV template" },
			CurrentFile = "TechQuest-Quiz-Template.csv",
			Title = "Save quiz template"
		};
		AddChild(importDialog);
		AddChild(exportDialog);

		subjectLabel.Text = $"Subject: {MultiplayerRoomState.RoomName}";
		roomCode.Text = MultiplayerRoomState.RoomCode;
		studentCountLabel.Text = "Students joined: 0";

		backButton.Pressed += OnBackPressed;
		uploadQuizButton.Pressed += () => uploadPopup.PopupCentered();
		downloadFormatButton.Pressed += () => exportDialog.PopupCentered();
		resultsButton.Pressed += OnResultsPressed;
		uploadFileButton.Pressed += () => importDialog.PopupCentered();
		uploadButton.Pressed += OnUploadPressed;
		cancelButton.Pressed += () => uploadPopup.Hide();
		importDialog.FileSelected += OnQuizFileSelected;
		exportDialog.FileSelected += OnTemplateSavePathSelected;
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
		GetTree().ChangeSceneToFile("res://scene/Dashboard/Modules/Room/ProfessorRoom.tscn");
	}

	private void OnResultsPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/Modules/Room/ProfessorRoomResult.tscn");
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
				card.GetNode<Label>("AvatarContainer/StudentNameLabel").Text = participant.Name;
				if (!string.IsNullOrWhiteSpace(participant.Avatar))
					avatar.Texture = await AccountApiClient.LoadAvatarAsync(participant.Avatar);
			}
		}
		catch (Exception exception)
		{
			GD.PrintErr($"Load professor room participants failed: {exception.Message}");
		}
		finally
		{
			refreshing = false;
		}
	}

	private void OnQuizFileSelected(string path)
	{
		selectedQuizPath = path;
		fileLabel.Text = System.IO.Path.GetFileName(path);
	}

	private async void OnUploadPressed()
	{
		if (string.IsNullOrWhiteSpace(selectedQuizPath))
		{
			ShowUploadMessage("Select a CSV quiz file first.");
			return;
		}
		if (!int.TryParse(timerInput.Text, out int timerSeconds) || timerSeconds < 1)
		{
			ShowUploadMessage("Enter a question timer of at least 1 second.");
			return;
		}

		try
		{
			List<RoomQuizQuestionDto> questions = ParseQuizCsv(FileAccess.GetFileAsString(selectedQuizPath));
			if (questions.Count == 0)
			{
				ShowUploadMessage("The CSV does not contain any complete questions.");
				return;
			}

			await RoomApiClient.SaveQuizAsync(MultiplayerRoomState.RoomId, System.IO.Path.GetFileName(selectedQuizPath), timerSeconds, questions);
			uploadPopup.Hide();
			selectedQuizPath = "";
			fileLabel.Text = "Quiz uploaded";
		}
		catch (Exception exception)
		{
			ShowUploadMessage(exception.Message);
		}
	}

	private List<RoomQuizQuestionDto> ParseQuizCsv(string csvText)
	{
		List<RoomQuizQuestionDto> questions = new();
		string[] lines = csvText.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
		for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
		{
			List<string> columns = ParseCsvLine(lines[lineIndex]);
			if (columns.Count < 8 || string.IsNullOrWhiteSpace(columns[2]))
				continue;

			string questionType = NormalizeQuestionType(columns[1]);
			if (string.IsNullOrWhiteSpace(questionType) || string.IsNullOrWhiteSpace(columns[7]))
				continue;
			questions.Add(new RoomQuizQuestionDto
			{
				Type = questionType,
				SubjectName = MultiplayerRoomState.RoomName,
				Question = columns[2],
				ChoiceA = columns[3],
				ChoiceB = columns[4],
				ChoiceC = columns[5],
				ChoiceD = columns[6],
				CorrectAnswer = columns[7]
			});
		}
		return questions;
	}

	private List<string> ParseCsvLine(string line)
	{
		List<string> columns = new();
		StringBuilder value = new();
		bool insideQuotes = false;
		for (int characterIndex = 0; characterIndex < line.Length; characterIndex++)
		{
			char character = line[characterIndex];
			if (character == '"')
			{
				if (insideQuotes && characterIndex + 1 < line.Length && line[characterIndex + 1] == '"')
				{
					value.Append('"');
					characterIndex++;
				}
				else
					insideQuotes = !insideQuotes;
			}
			else if (character == ',' && !insideQuotes)
			{
				columns.Add(value.ToString().Trim());
				value.Clear();
			}
			else
				value.Append(character);
		}
		columns.Add(value.ToString().Trim());
		return columns;
	}

	private string NormalizeQuestionType(string value)
	{
		string normalized = value.Trim().ToLowerInvariant();
		if (normalized.Contains("multiple") || normalized == "mcq")
			return "Multiple Choice";
		if (normalized.Contains("true") || normalized == "t/f")
			return "True or False";
		if (normalized.Contains("identification") || normalized == "id")
			return "Identification";
		return "";
	}

	private void OnTemplateSavePathSelected(string path)
	{
		try
		{
			string template = FileAccess.GetFileAsString("res://asset/QuizTemplate/TechQuest-Quiz-Template.csv");
			using FileAccess output = FileAccess.Open(path, FileAccess.ModeFlags.Write);
			output.StoreString(template);
		}
		catch (Exception exception)
		{
			ShowUploadMessage(exception.Message);
		}
	}

	private void ShowUploadMessage(string message)
	{
		AcceptDialog dialog = new() { Title = "Room quiz" };
		AddChild(dialog);
		dialog.DialogText = message;
		dialog.PopupCentered();
	}
}
