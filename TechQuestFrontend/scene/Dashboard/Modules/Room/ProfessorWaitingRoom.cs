
using Godot;
using ClosedXML.Excel;
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
	private Panel uploadPopup;
	private GridContainer avatarGrid;
	private Control avatarTemplate;
	private FileDialog importDialog;
	private FileDialog exportDialog;
	private AcceptDialog messageDialog;

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

		uploadPopup = GetNode<Panel>("UploadQuizPopup");
		fileLabel = GetNode<Label>(
            "UploadQuizPopup/UploadQuizContainer/FileLabel"
		);
		timerInput = GetNode<LineEdit>(
            "UploadQuizPopup/UploadQuizContainer/TimerInput"
		);
		uploadFileButton = GetNode<Button>(
            "UploadQuizPopup/UploadQuizContainer/UploadFileButton"
		);
		uploadButton = GetNode<Button>(
            "UploadQuizPopup/UploadQuizContainer/ButtonContainer/UploadButton"
		);
		cancelButton = GetNode<Button>(
            "UploadQuizPopup/UploadQuizContainer/ButtonContainer/CancelButton"
		);

		avatarGrid = GetNode<GridContainer>(
            "AvatarPanel/StudentScrool/AvatarGridContianer"
		);

		avatarTemplate = avatarGrid.GetChild<Control>(0);
		avatarTemplate.Visible = false;

		if (OS.GetName() != "Android")
		{
			// Desktop file pickers are intentionally not created on Android.
			importDialog = new FileDialog
			{
				Title = "Choose Quiz File",
				FileMode = FileDialog.FileModeEnum.OpenFile,
				Access = FileDialog.AccessEnum.Filesystem,
				UseNativeDialog = true,
				Filters = new string[]
				{
					"*.xlsx ; Excel Quiz",
                    "*.csv ; CSV Quiz"
				}
			};

			AddChild(importDialog);
			importDialog.FileSelected += OnQuizFileSelected;

			exportDialog = new FileDialog
			{
				Title = "Save Quiz Template",
				FileMode = FileDialog.FileModeEnum.SaveFile,
				Access = FileDialog.AccessEnum.Filesystem,
				UseNativeDialog = true,
				Filters = new string[]
				{
                    "*.xlsx ; Excel Template"
				},
				CurrentFile = "TechQuest-Quiz-Template.xlsx"
			};

			AddChild(exportDialog);
			exportDialog.FileSelected += OnTemplateSavePathSelected;
		}

		messageDialog = new AcceptDialog
		{
			Title = "Room quiz"
		};
		AddChild(messageDialog);

		subjectLabel.Text = $"Subject: {MultiplayerRoomState.RoomName}";
		roomCode.Text = MultiplayerRoomState.RoomCode;
		studentCountLabel.Text = "Students joined: 0";

		backButton.Pressed += OnBackPressed;
		uploadQuizButton.Pressed += uploadPopup.Show;
		downloadFormatButton.Pressed += () => exportDialog?.PopupCentered();
		resultsButton.Pressed += OnResultsPressed;
		uploadFileButton.Pressed += () => importDialog?.PopupCentered();
		uploadButton.Pressed += OnUploadPressed;
		cancelButton.Pressed += () => uploadPopup.Hide();

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
		GetTree().ChangeSceneToFile(
            "res://scene/Dashboard/Modules/Room/ProfessorRoom.tscn"
		);
	}

	private void OnResultsPressed()
	{
		GetTree().ChangeSceneToFile(
            "res://scene/Dashboard/Modules/Room/ProfessorRoomResult.tscn"
		);
	}

	private async Task RefreshParticipantsAsync()
	{
		if (refreshing)
			return;

		refreshing = true;

		try
		{
			List<RoomParticipantDto> participants =
				await RoomApiClient.GetParticipantsAsync(
					MultiplayerRoomState.RoomId
				);

			studentCountLabel.Text =
				$"Students joined: {participants.Count}";

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

				TextureRect avatar =
					card.GetNode<TextureRect>(
                        "AvatarContainer/AvatarTexture"
					);

				card.GetNode<Label>(
                    "AvatarContainer/StudentNameLabel"
				).Text = participant.Name;

				if (!string.IsNullOrWhiteSpace(participant.Avatar))
				{
					avatar.Texture =
						await AccountApiClient.LoadAvatarAsync(
							participant.Avatar
						);
				}
			}
		}
		catch (Exception exception)
		{
			GD.PrintErr(
				$"Load professor room participants failed: {exception.Message}"
			);
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
			ShowMessage(
				"Upload error",
                "Select an Excel or CSV quiz file first."
			);
			return;
		}

		if (!TryParseTimerSeconds(timerInput.Text, out int timerSeconds))
		{
			ShowMessage(
				"Upload error",
				"Enter the timer as a number followed by seconds or minutes, " +
                "such as 5 seconds or 5 minutes."
			);
			return;
		}

		try
		{
			string uploadedFileName =
				System.IO.Path.GetFileName(selectedQuizPath);

			List<RoomQuizQuestionDto> questions;

			if (System.IO.Path.GetExtension(selectedQuizPath)
				.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
			{
				questions = ParseQuizXlsx(selectedQuizPath);
			}
			else if (System.IO.Path.GetExtension(selectedQuizPath)
				.Equals(".csv", StringComparison.OrdinalIgnoreCase))
			{
				questions = ParseQuizCsv(
					System.IO.File.ReadAllText(selectedQuizPath)
				);
			}
			else
			{
				ShowMessage(
					"Upload error",
                    "Unsupported file type. Choose an XLSX or CSV file."
				);
				return;
			}

			if (questions.Count == 0)
			{
				ShowMessage(
					"Upload error",
                    "The file does not contain any complete questions."
				);
				return;
			}

			await RoomApiClient.SaveQuizAsync(
				MultiplayerRoomState.RoomId,
				uploadedFileName,
				timerSeconds,
				questions
			);

			selectedQuizPath = "";
			fileLabel.Text = $"Uploaded: {uploadedFileName}";

			ShowMessage(
				"Upload successful",
				$"{uploadedFileName} was uploaded successfully."
			);
		}
		catch (Exception exception)
		{
			ShowMessage("Upload error", exception.Message);
		}
	}

	private bool TryParseTimerSeconds(
		string value,
		out int timerSeconds
	)
	{
		timerSeconds = 0;

		string[] parts = value.Trim().Split(
			' ',
			StringSplitOptions.RemoveEmptyEntries
		);

		if (parts.Length != 2 ||
			!int.TryParse(parts[0], out int amount) ||
			amount < 1)
		{
			return false;
		}

		if (parts[1].Equals(
			"second",
			StringComparison.OrdinalIgnoreCase
		) ||
			parts[1].Equals(
				"seconds",
				StringComparison.OrdinalIgnoreCase
			))
		{
			timerSeconds = amount;
			return true;
		}

		if (parts[1].Equals(
			"minute",
			StringComparison.OrdinalIgnoreCase
		) ||
			parts[1].Equals(
				"minutes",
				StringComparison.OrdinalIgnoreCase
			))
		{
			if (amount > int.MaxValue / 60)
				return false;

			timerSeconds = amount * 60;
			return true;
		}

		return false;
	}

	private List<RoomQuizQuestionDto> ParseQuizCsv(string csvText)
	{
		List<RoomQuizQuestionDto> questions = new();

		string[] lines = csvText
			.Replace("\r", "")
			.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
		{
			List<string> columns = ParseCsvLine(lines[lineIndex]);

			if (columns.Count < 8 ||
				string.IsNullOrWhiteSpace(columns[2]))
			{
				continue;
			}

			string questionType = NormalizeQuestionType(columns[1]);

			if (string.IsNullOrWhiteSpace(questionType) ||
				string.IsNullOrWhiteSpace(columns[7]))
			{
				continue;
			}

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

	private List<RoomQuizQuestionDto> ParseQuizXlsx(string path)
	{
		List<RoomQuizQuestionDto> questions = new();

		using XLWorkbook workbook = new(path);
		IXLWorksheet worksheet = workbook.Worksheet(1);

		foreach (IXLRow row in worksheet.RowsUsed())
		{
			if (row.RowNumber() == 1)
				continue;

			string question = row.Cell(3).GetString().Trim();
			string questionType =
				NormalizeQuestionType(row.Cell(2).GetString());
			string correctAnswer = row.Cell(8).GetString().Trim();

			if (string.IsNullOrWhiteSpace(question) ||
				string.IsNullOrWhiteSpace(questionType) ||
				string.IsNullOrWhiteSpace(correctAnswer))
			{
				continue;
			}

			questions.Add(new RoomQuizQuestionDto
			{
				Type = questionType,
				SubjectName = MultiplayerRoomState.RoomName,
				Question = question,
				ChoiceA = row.Cell(4).GetString().Trim(),
				ChoiceB = row.Cell(5).GetString().Trim(),
				ChoiceC = row.Cell(6).GetString().Trim(),
				ChoiceD = row.Cell(7).GetString().Trim(),
				CorrectAnswer = correctAnswer
			});
		}

		return questions;
	}

	private List<string> ParseCsvLine(string line)
	{
		List<string> columns = new();
		StringBuilder value = new();
		bool insideQuotes = false;

		for (int characterIndex = 0;
			 characterIndex < line.Length;
			 characterIndex++)
		{
			char character = line[characterIndex];

			if (character == '"')
			{
				if (insideQuotes &&
					characterIndex + 1 < line.Length &&
					line[characterIndex + 1] == '"')
				{
					value.Append('"');
					characterIndex++;
				}
				else
				{
					insideQuotes = !insideQuotes;
				}
			}
			else if (character == ',' && !insideQuotes)
			{
				columns.Add(value.ToString().Trim());
				value.Clear();
			}
			else
			{
				value.Append(character);
			}
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
			byte[] template = FileAccess.GetFileAsBytes(
                "res://asset/QuizTemplate/TechQuest-Quiz-Template.xlsx"
			);

			if (template.Length == 0)
			{
				throw new Exception(
                    "The quiz template could not be loaded."
				);
			}

			using FileAccess output = FileAccess.Open(
				path,
				FileAccess.ModeFlags.Write
			);

			if (output == null)
			{
				throw new Exception(
					$"Could not save the template to {path}."
				);
			}

			output.StoreBuffer(template);
			output.Flush();

			if (output.GetError() != Error.Ok)
			{
				throw new Exception(
					$"The template could not be saved to {path}."
				);
			}

			ShowMessage(
				"Download successful",
				$"Template downloaded successfully to:\n{path}"
			);
		}
		catch (Exception exception)
		{
			ShowMessage("Download error", exception.Message);
		}
	}

	private void ShowMessage(string title, string message)
	{
		messageDialog.Title = title;
		messageDialog.DialogText = message;
		messageDialog.PopupCentered();
	}
}
