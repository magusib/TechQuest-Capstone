using Godot;
using System;
using System.Threading.Tasks;

public partial class StudentRoomResult : Control
{
	private Label subjectLabel;
	private Label studentLabel;
	private Label scoreLabel;
	private Label correctLabel;
	private Label incorrectLabel;
	private Label percentageLabel;
	private Label reviewSubjectLabel;
	private Label reviewScoreLabel;
	private Panel reviewPanel;
	private VBoxContainer reviewContainer;
	private Panel multipleChoiceTemplate;
	private Panel trueFalseTemplate;
	private Panel identificationTemplate;
	private AcceptDialog alertDialog;

	public override void _Ready()
	{
		subjectLabel = GetNode<Label>("RoomResult/VBoxContainer/SubjectNameLabel");
		studentLabel = GetNode<Label>("RoomResult/VBoxContainer/StudentNameLabel");
		scoreLabel = GetNode<Label>("RoomResult/VBoxContainer/ScoreLabel");
		correctLabel = GetNode<Label>("RoomResult/VBoxContainer/CorrectLabel");
		incorrectLabel = GetNode<Label>("RoomResult/VBoxContainer/IncorrectLabel");
		percentageLabel = GetNode<Label>("RoomResult/VBoxContainer/PercentageLabel");
		reviewPanel = GetNode<Panel>("QuizReview");
		reviewContainer = GetNode<VBoxContainer>("QuizReview/ScrollContainer/VBoxContainer");
		reviewSubjectLabel = GetNode<Label>("QuizReview/ScrollContainer/VBoxContainer/SubjectNameLabel");
		reviewScoreLabel = GetNode<Label>("QuizReview/ScrollContainer/VBoxContainer/ScoreLabel");
		multipleChoiceTemplate = GetNode<Panel>("QuizReview/ScrollContainer/VBoxContainer/MultipleChoicesPanel");
		trueFalseTemplate = GetNode<Panel>("QuizReview/ScrollContainer/VBoxContainer/TrueOrFalsePanel");
		identificationTemplate = GetNode<Panel>("QuizReview/ScrollContainer/VBoxContainer/IdentificationPanel");
		alertDialog = new AcceptDialog { Title = "Quiz result" };
		AddChild(alertDialog);

		multipleChoiceTemplate.Visible = false;
		trueFalseTemplate.Visible = false;
		identificationTemplate.Visible = false;
		GetNode<Button>("RoomResult/VBoxContainer/ReviewButton").Pressed += reviewPanel.Show;
		GetNode<Button>("QuizReview/CloseButton").Pressed += reviewPanel.Hide;
		GetNode<Button>("BackButton").Pressed += OnBackPressed;
		GetNode<Button>("RoomResult/VBoxContainer/BackButton").Pressed += OnBackPressed;
		_ = LoadResultAsync();
	}

	private async Task LoadResultAsync()
	{
		int reportedScore = MultiplayerRoomState.Score;
		int reportedTotal = MultiplayerRoomState.Questions.Count;
		try
		{
			RoomResultDto result = await RoomApiClient.GetMyResultAsync(MultiplayerRoomState.RoomId);
			reportedScore = result.Score;
			reportedTotal = result.TotalQuestions;
			MultiplayerRoomState.Score = reportedScore;
			MultiplayerRoomState.Answers = result.Answers ?? new();
		}
		catch (Exception exception)
		{
			GD.PrintErr($"Load student result failed: {exception.Message}");
			if (MultiplayerRoomState.Answers.Count == 0)
			{
				alertDialog.DialogText = exception.Message;
				alertDialog.PopupCentered();
			}
		}

		int total = reportedTotal > 0 ? reportedTotal : MultiplayerRoomState.Answers.Count;
		int correct = 0;
		foreach (RoomAnswerDto answer in MultiplayerRoomState.Answers)
		{
			if (answer.IsCorrect)
				correct++;
			AddReviewItem(answer);
		}
		if (MultiplayerRoomState.Answers.Count == 0)
			correct = reportedScore;

		int incorrect = total - correct;
		string subjectName = MultiplayerRoomState.RoomName;
		if (string.IsNullOrWhiteSpace(subjectName))
		{
			foreach (RoomAnswerDto answer in MultiplayerRoomState.Answers)
			{
				if (!string.IsNullOrWhiteSpace(answer.SubjectName))
				{
					subjectName = answer.SubjectName;
					break;
				}
			}
		}

		subjectLabel.Text = $"Subject: {subjectName}";
		studentLabel.Text = "Student result";
		scoreLabel.Text = $"Score: {reportedScore}/{total}";
		correctLabel.Text = $"Correct answers: {correct}";
		incorrectLabel.Text = $"Incorrect answers: {incorrect}";
		percentageLabel.Text = $"Percentage: {(total == 0 ? 0 : correct * 100 / total)}%";
		reviewSubjectLabel.Text = $"Subject: {subjectName}";
		reviewScoreLabel.Text = scoreLabel.Text;
	}

	private void AddReviewItem(RoomAnswerDto answer)
	{
		Panel template = answer.Type.Equals("Multiple Choice", StringComparison.OrdinalIgnoreCase)
			? multipleChoiceTemplate
			: answer.Type.Equals("True or False", StringComparison.OrdinalIgnoreCase)
				|| answer.Type.Equals("True/False", StringComparison.OrdinalIgnoreCase)
				? trueFalseTemplate
				: identificationTemplate;
		Panel item = (Panel)template.Duplicate();
		item.Visible = true;
		reviewContainer.AddChild(item);

		if (template == multipleChoiceTemplate)
		{
			VBoxContainer content = item.GetNode<VBoxContainer>("MultipleChoicesContainer");
			content.GetNode<Label>("QuestionLabel").Text = $"Question: {answer.Question}";
			SetChoiceLabel(content.GetNode<Label>("ChoicesA Label"), "A", answer.ChoiceA, answer);
			SetChoiceLabel(content.GetNode<Label>("ChoicesB Label"), "B", answer.ChoiceB, answer);
			SetChoiceLabel(content.GetNode<Label>("ChoicesC Label"), "C", answer.ChoiceC, answer);
			SetChoiceLabel(content.GetNode<Label>("ChoicesD Label"), "D", answer.ChoiceD, answer);
			content.GetNode<Label>("YourAnswerLabel").Text = $"Your answer: {answer.StudentAnswer}";
			content.GetNode<Label>("CorrectAnswerLabel").Text = $"Correct answer: {GetDisplayCorrectAnswer(answer)}";
		}
		else if (template == trueFalseTemplate)
		{
			VBoxContainer content = item.GetNode<VBoxContainer>("TrueOrFalseContainer");
			content.GetNode<Label>("QuestionLabel").Text = $"Question: {answer.Question}";
			content.GetNode<Label>("TrueLabel").Text = answer.StudentAnswer.Equals("True", StringComparison.OrdinalIgnoreCase) ? "True (your answer)" : "True";
			content.GetNode<Label>("FalseLabel").Text = answer.StudentAnswer.Equals("False", StringComparison.OrdinalIgnoreCase) ? "False (your answer)" : "False";
			content.GetNode<Label>("YourAnswerLabel").Text = $"Your answer: {answer.StudentAnswer}";
			content.GetNode<Label>("CorrectAnswerLabel").Text = $"Correct answer: {answer.CorrectAnswer}";
		}
		else
		{
			VBoxContainer content = item.GetNode<VBoxContainer>("IdentificationContainer");
			content.GetNode<Label>("QuestionLabel").Text = $"Question: {answer.Question}";
			content.GetNode<Label>("YourAnswerLabel").Text = $"Your answer: {answer.StudentAnswer}";
			content.GetNode<Label>("CorrectAnswerLabel").Text = $"Correct answer: {answer.CorrectAnswer}";
		}
	}

	private void SetChoiceLabel(Label label, string key, string choice, RoomAnswerDto answer)
	{
		string display = $"{key}. {choice}";
		if (string.Equals(choice, answer.StudentAnswer, StringComparison.OrdinalIgnoreCase))
			display += " (your answer)";
		if (IsCorrectChoice(choice, key, answer))
			display += " (correct)";
		label.Text = display;
		label.Visible = !string.IsNullOrWhiteSpace(choice);
	}

	private bool IsCorrectChoice(string choice, string key, RoomAnswerDto answer)
	{
		string expected = answer.CorrectAnswer.Trim();
		return string.Equals(choice, expected, StringComparison.OrdinalIgnoreCase)
			|| expected.Equals(key, StringComparison.OrdinalIgnoreCase)
			|| expected.Equals($"Choice {key}", StringComparison.OrdinalIgnoreCase);
	}

	private string GetDisplayCorrectAnswer(RoomAnswerDto answer)
	{
		string key = answer.CorrectAnswer.Trim().ToUpperInvariant();
		return key switch
		{
			"A" or "CHOICE A" => answer.ChoiceA,
			"B" or "CHOICE B" => answer.ChoiceB,
			"C" or "CHOICE C" => answer.ChoiceC,
			"D" or "CHOICE D" => answer.ChoiceD,
			_ => answer.CorrectAnswer
		};
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/UserDashboard/Student Dashboard/StudentDashboard.tscn");
	}

}
