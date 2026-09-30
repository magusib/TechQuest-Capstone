using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class StudentQuizSession : Control
{
	private Label subjectLabel;
	private Label questionNumberLabel;
	private Label timerLabel;
	private Label questionLabel;
	private VBoxContainer multipleChoicesContainer;
	private VBoxContainer trueOrFalseContainer;
	private VBoxContainer identificationContainer;
	private LineEdit identificationAnswer;
	private AcceptDialog alertDialog;
	private List<Button> answerButtons = new();
	private int questionIndex;
	private float timeRemaining;
	private float feedbackRemaining;
	private bool showingFeedback;
	private bool finishingQuiz;

	public override void _Ready()
	{
		subjectLabel = GetNode<Label>("QuestionHeaderPanel/SubjectNameLabel");
		questionNumberLabel = GetNode<Label>("QuestionHeaderPanel/NumberOfQuestionLabel");
		timerLabel = GetNode<Label>("QuestionHeaderPanel/TimerLabel");
		questionLabel = GetNode<Label>("QuizPanel/QuestionLabel");
		multipleChoicesContainer = GetNode<VBoxContainer>("QuizPanel/MultipleChoicesContainer");
		trueOrFalseContainer = GetNode<VBoxContainer>("QuizPanel/TrueorFalseContainer");
		identificationContainer = GetNode<VBoxContainer>("QuizPanel/IdentificationContainer");
		identificationAnswer = GetNode<LineEdit>("QuizPanel/IdentificationContainer/IndentificationAnswer");
		alertDialog = new AcceptDialog { Title = "Quiz" };
		AddChild(alertDialog);

		answerButtons.Add(GetNode<Button>("QuizPanel/MultipleChoicesContainer/ChoicesA Button"));
		answerButtons.Add(GetNode<Button>("QuizPanel/MultipleChoicesContainer/ChoicesB Button"));
		answerButtons.Add(GetNode<Button>("QuizPanel/MultipleChoicesContainer/ChoicesC Button"));
		answerButtons.Add(GetNode<Button>("QuizPanel/MultipleChoicesContainer/ChoicesD Button"));
		answerButtons[0].Pressed += () => SubmitAnswer(MultiplayerRoomState.Questions[questionIndex].ChoiceA);
		answerButtons[1].Pressed += () => SubmitAnswer(MultiplayerRoomState.Questions[questionIndex].ChoiceB);
		answerButtons[2].Pressed += () => SubmitAnswer(MultiplayerRoomState.Questions[questionIndex].ChoiceC);
		answerButtons[3].Pressed += () => SubmitAnswer(MultiplayerRoomState.Questions[questionIndex].ChoiceD);
		GetNode<Button>("QuizPanel/TrueorFalseContainer/TrueButton").Pressed += () => SubmitAnswer("True");
		GetNode<Button>("QuizPanel/TrueorFalseContainer/FalseButton").Pressed += () => SubmitAnswer("False");
		GetNode<Button>("QuizPanel/IdentificationContainer/SubmitButton").Pressed += OnIdentificationSubmit;

		_ = LoadQuizAsync();
	}

	public override void _Process(double delta)
	{
		if (finishingQuiz || MultiplayerRoomState.Questions.Count == 0)
			return;

		if (showingFeedback)
		{
			feedbackRemaining -= (float)delta;
			if (feedbackRemaining <= 0)
				ShowQuestion(questionIndex + 1);
			return;
		}

		timeRemaining -= (float)delta;
		timerLabel.Text = $"Time: {Math.Max(0, (int)Math.Ceiling(timeRemaining)):00}";
		if (timeRemaining <= 0)
		{
			alertDialog.DialogText = "Time's up.";
			alertDialog.PopupCentered();
			RecordAnswer("");
			ShowQuestion(questionIndex + 1);
		}
	}

	private async Task LoadQuizAsync()
	{
		try
		{
			RoomQuizDto quiz = await RoomApiClient.GetQuizAsync(MultiplayerRoomState.RoomId);
			MultiplayerRoomState.SetQuiz(quiz.TimerSeconds, quiz.Questions);
			if (MultiplayerRoomState.Questions.Count == 0)
			{
				alertDialog.DialogText = "No questions are available for this room.";
				alertDialog.PopupCentered();
				return;
			}

			subjectLabel.Text = MultiplayerRoomState.Questions[0].SubjectName;
			ShowQuestion(0);
		}
		catch (Exception exception)
		{
			alertDialog.DialogText = exception.Message;
			alertDialog.PopupCentered();
		}
	}

	private void ShowQuestion(int index)
	{
		if (index >= MultiplayerRoomState.Questions.Count)
		{
			_ = FinishQuizAsync();
			return;
		}

		questionIndex = index;
		RoomQuizQuestionDto question = MultiplayerRoomState.Questions[questionIndex];
		questionNumberLabel.Text = $"Question: {questionIndex + 1}/{MultiplayerRoomState.Questions.Count}";
		questionLabel.Text = question.Question;
		timeRemaining = MultiplayerRoomState.TimerSeconds;
		showingFeedback = false;
		identificationAnswer.Text = "";
		multipleChoicesContainer.Visible = question.Type.Equals("Multiple Choice", StringComparison.OrdinalIgnoreCase);
		trueOrFalseContainer.Visible = question.Type.Equals("True or False", StringComparison.OrdinalIgnoreCase)
			|| question.Type.Equals("True/False", StringComparison.OrdinalIgnoreCase);
		identificationContainer.Visible = question.Type.Equals("Identification", StringComparison.OrdinalIgnoreCase);
		SetAnswerControlsEnabled(true);

		answerButtons[0].Text = question.ChoiceA;
		answerButtons[1].Text = question.ChoiceB;
		answerButtons[2].Text = question.ChoiceC;
		answerButtons[3].Text = question.ChoiceD;
		for (int buttonIndex = 0; buttonIndex < answerButtons.Count; buttonIndex++)
			answerButtons[buttonIndex].Visible = !string.IsNullOrWhiteSpace(answerButtons[buttonIndex].Text);
	}

	private void OnIdentificationSubmit()
	{
		if (!string.IsNullOrWhiteSpace(identificationAnswer.Text))
			SubmitAnswer(identificationAnswer.Text.Trim());
	}

	private void SubmitAnswer(string answer)
	{
		if (showingFeedback || finishingQuiz || MultiplayerRoomState.Questions.Count == 0)
			return;

		RecordAnswer(answer);
		RoomAnswerDto result = MultiplayerRoomState.Answers[^1];
		string correctAnswer = GetDisplayCorrectAnswer(result);
		questionLabel.Text = result.IsCorrect
			? $"Correct! {correctAnswer}"
			: $"Incorrect. Correct answer: {correctAnswer}";
		SetAnswerControlsEnabled(false);
		showingFeedback = true;
		feedbackRemaining = 1.5f;
	}

	private void RecordAnswer(string answer)
	{
		RoomQuizQuestionDto question = MultiplayerRoomState.Questions[questionIndex];
		bool correct = IsCorrectAnswer(answer, question);
		MultiplayerRoomState.Answers.Add(new RoomAnswerDto
		{
			Type = question.Type,
			SubjectName = question.SubjectName,
			Question = question.Question,
			ChoiceA = question.ChoiceA,
			ChoiceB = question.ChoiceB,
			ChoiceC = question.ChoiceC,
			ChoiceD = question.ChoiceD,
			CorrectAnswer = question.CorrectAnswer,
			StudentAnswer = answer,
			IsCorrect = correct
		});
		if (correct)
			MultiplayerRoomState.Score++;
	}

	private bool IsCorrectAnswer(string answer, RoomQuizQuestionDto question)
	{
		string expected = question.CorrectAnswer.Trim();
		if (string.Equals(answer.Trim(), expected, StringComparison.OrdinalIgnoreCase))
			return true;

		string key = expected.ToUpperInvariant();
		string choice = key switch
		{
			"A" or "CHOICE A" => question.ChoiceA,
			"B" or "CHOICE B" => question.ChoiceB,
			"C" or "CHOICE C" => question.ChoiceC,
			"D" or "CHOICE D" => question.ChoiceD,
			_ => ""
		};
		return !string.IsNullOrWhiteSpace(choice)
			&& string.Equals(answer.Trim(), choice.Trim(), StringComparison.OrdinalIgnoreCase);
	}

	private string GetDisplayCorrectAnswer(RoomQuizQuestionDto question)
	{
		string key = question.CorrectAnswer.Trim().ToUpperInvariant();
		return key switch
		{
			"A" or "CHOICE A" => question.ChoiceA,
			"B" or "CHOICE B" => question.ChoiceB,
			"C" or "CHOICE C" => question.ChoiceC,
			"D" or "CHOICE D" => question.ChoiceD,
			_ => question.CorrectAnswer
		};
	}

	private void SetAnswerControlsEnabled(bool enabled)
	{
		foreach (Button button in answerButtons)
			button.Disabled = !enabled;
		GetNode<Button>("QuizPanel/TrueorFalseContainer/TrueButton").Disabled = !enabled;
		GetNode<Button>("QuizPanel/TrueorFalseContainer/FalseButton").Disabled = !enabled;
		GetNode<Button>("QuizPanel/IdentificationContainer/SubmitButton").Disabled = !enabled;
	}

	private async Task FinishQuizAsync()
	{
		if (finishingQuiz)
			return;

		finishingQuiz = true;
		SetAnswerControlsEnabled(false);
		try
		{
			await RoomApiClient.SaveResultAsync(
				MultiplayerRoomState.RoomId,
				MultiplayerRoomState.Score,
				MultiplayerRoomState.Questions.Count,
				MultiplayerRoomState.Answers);
		}
		catch (Exception exception)
		{
			GD.PrintErr($"Save room quiz result failed: {exception.Message}");
		}

		GetTree().ChangeSceneToFile("res://scene/Dashboard/Modules/Room/StudentRoomResult.tscn");
	}
}
