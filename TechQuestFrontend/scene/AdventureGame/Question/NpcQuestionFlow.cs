using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public abstract partial class NpcQuestionFlow : Control
{
	private readonly List<Button> answerButtons = new();
	private StudentChallengeQuestionDto question;
	private Timer countdown;
	private Label timerLabel;
	private Label statusLabel;
	private Button submitButton;
	private Button retryButton;
	private LineEdit answerInput;
	private string selectedAnswer = "";
	private string pendingAnswer = "";
	private int timeLeft;
	private bool isSubmitting;
	private bool isFinalized;

	protected abstract string Difficulty { get; }

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Hide();
	}

	public async Task ShowQuestionAsync(StudentChallengeQuestionDto assignedQuestion)
	{
		if (assignedQuestion == null ||
			assignedQuestion.DeploymentId <= 0 ||
			!string.Equals(assignedQuestion.Difficulty, Difficulty, StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException("The NPC question configuration is incomplete or does not match this map.");

		question = assignedQuestion;
		isFinalized = false;
		isSubmitting = false;
		selectedAnswer = "";
		pendingAnswer = "";
		timeLeft = Math.Max(0, question.RemainingSeconds);
		BuildQuestionScreen();
		Show();

		if (timeLeft == 0)
			await SubmitAnswerAsync("");
		else
			countdown.Start();
	}

	private void BuildQuestionScreen()
	{
		RemoveExistingControls();
		answerButtons.Clear();
		answerInput = null;

		var backdrop = new ColorRect
		{
			Color = new Color(0, 0, 0, 0.72f),
			MouseFilter = MouseFilterEnum.Stop
		};
		backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(backdrop);

		var panel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(700, 560),
			MouseFilter = MouseFilterEnum.Stop
		};
		panel.SetAnchorsPreset(LayoutPreset.Center);
		panel.OffsetLeft = -350;
		panel.OffsetTop = -280;
		panel.OffsetRight = 350;
		panel.OffsetBottom = 280;
		AddChild(panel);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 24);
		margin.AddThemeConstantOverride("margin_top", 20);
		margin.AddThemeConstantOverride("margin_right", 24);
		margin.AddThemeConstantOverride("margin_bottom", 20);
		panel.AddChild(margin);

		var layout = new VBoxContainer();
		layout.AddThemeConstantOverride("separation", 12);
		margin.AddChild(layout);
		layout.AddChild(CreateLabel(question.SubjectName, 25, HorizontalAlignment.Center));

		timerLabel = CreateLabel("", 20, HorizontalAlignment.Center);
		layout.AddChild(timerLabel);
		UpdateTimerLabel();

		var questionLabel = CreateLabel(question.Question, 22, HorizontalAlignment.Left);
		questionLabel.AutowrapMode = TextServer.AutowrapMode.Word;
		questionLabel.CustomMinimumSize = new Vector2(0, 70);
		layout.AddChild(questionLabel);

		if (question.AnswerChoices != null && question.AnswerChoices.Count > 0)
		{
			var choices = new VBoxContainer();
			choices.AddThemeConstantOverride("separation", 8);
			layout.AddChild(choices);
			var buttonGroup = new ButtonGroup();
			foreach (string choice in question.AnswerChoices)
			{
				var answerButton = new Button
				{
					Text = choice,
					ToggleMode = true,
					ButtonGroup = buttonGroup,
					CustomMinimumSize = new Vector2(0, 42)
				};
				answerButton.Pressed += () => selectedAnswer = choice;
				answerButtons.Add(answerButton);
				choices.AddChild(answerButton);
			}
		}
		else
		{
			answerInput = new LineEdit
			{
				PlaceholderText = "Type your answer here",
				CustomMinimumSize = new Vector2(0, 44)
			};
			layout.AddChild(answerInput);
		}

		statusLabel = CreateLabel("", 16, HorizontalAlignment.Center);
		statusLabel.AutowrapMode = TextServer.AutowrapMode.Word;
		layout.AddChild(statusLabel);

		submitButton = new Button
		{
			Text = "Submit Answer",
			CustomMinimumSize = new Vector2(0, 44)
		};
		submitButton.Pressed += SubmitSelectedAnswer;
		layout.AddChild(submitButton);

		retryButton = new Button
		{
			Text = "Retry Submission",
			CustomMinimumSize = new Vector2(0, 44),
			Visible = false
		};
		retryButton.Pressed += RetryAnswerSubmission;
		layout.AddChild(retryButton);

		countdown = new Timer
		{
			WaitTime = 1,
			OneShot = false,
			ProcessMode = ProcessModeEnum.Always
		};
		countdown.Timeout += OnCountdownTimeout;
		AddChild(countdown);
	}

	private async void OnCountdownTimeout()
	{
		if (isSubmitting || isFinalized)
			return;

		timeLeft--;
		UpdateTimerLabel();
		if (timeLeft <= 0)
		{
			countdown.Stop();
			await SubmitAnswerAsync("");
		}
	}

	private async void SubmitSelectedAnswer()
	{
		await SubmitAnswerAsync(answerInput?.Text ?? selectedAnswer);
	}

	private async void RetryAnswerSubmission()
	{
		await SubmitAnswerAsync(pendingAnswer);
	}

	private async Task SubmitAnswerAsync(string answer)
	{
		if (isSubmitting || isFinalized)
			return;
		if (timeLeft > 0 && string.IsNullOrWhiteSpace(answer))
		{
			statusLabel.Text = answerInput == null
				? "Select an answer before submitting."
				: "Type your answer before submitting.";
			return;
		}

		pendingAnswer = answer;
		isSubmitting = true;
		countdown?.Stop();
		SetAnswerControlsDisabled(true);
		statusLabel.Text = "Confirming your result with the server...";
		retryButton.Hide();
		try
		{
			QuestionAttemptResultDto result =
				await MiniGameApiClient.SubmitQuestionAttemptAsync(question.DeploymentId, answer);
			if (!result.Completed)
				throw new InvalidOperationException("The server did not finalize this question attempt.");

			isFinalized = true;
			UserSession.UpdateStudentProfile(result.TotalCoins, result.TotalPoints);
			try
			{
				LifespanResponseDto lifespan = await LifespanApiClient.GetAsync();
				UserSession.UpdateLifespan(lifespan.Lifespan);
			}
			catch (Exception lifespanException)
			{
				GD.PrintErr("Could not refresh lifespan after quiz: " + lifespanException.Message);
			}
			NpcMapSprite.RemoveDeploymentSprite(GetTree().CurrentScene, question.DeploymentId);
			ShowResult(result);
		}
		catch (Exception exception)
		{
			statusLabel.Text = "The result could not be confirmed. " + exception.Message;
			retryButton.Show();
		}
		finally
		{
			isSubmitting = false;
		}
	}

	private void ShowResult(QuestionAttemptResultDto result)
	{
		countdown?.Stop();
		countdown = null;
		RemoveExistingControls();
		var backdrop = new ColorRect
		{
			Color = new Color(0, 0, 0, 0.72f),
			MouseFilter = MouseFilterEnum.Stop
		};
		backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(backdrop);

		var panel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(620, 480),
			MouseFilter = MouseFilterEnum.Stop
		};
		panel.SetAnchorsPreset(LayoutPreset.Center);
		panel.OffsetLeft = -310;
		panel.OffsetTop = -240;
		panel.OffsetRight = 310;
		panel.OffsetBottom = 240;
		AddChild(panel);

		var layout = new VBoxContainer();
		layout.AddThemeConstantOverride("separation", 18);
		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 28);
		margin.AddThemeConstantOverride("margin_top", 24);
		margin.AddThemeConstantOverride("margin_right", 28);
		margin.AddThemeConstantOverride("margin_bottom", 24);
		margin.AddChild(layout);
		panel.AddChild(margin);

		layout.AddChild(CreateLabel(
			result.TimedOut ? "Time is up" : result.IsCorrect ? "Correct answer!" : "Incorrect answer",
			28,
			HorizontalAlignment.Center));
		layout.AddChild(CreateLabel("Your answer: " +
			(string.IsNullOrEmpty(result.SelectedAnswer) ? "No answer (timed out)" : result.SelectedAnswer),
			18,
			HorizontalAlignment.Left));
		layout.AddChild(CreateLabel("Correct answer: " + result.CorrectAnswer, 18, HorizontalAlignment.Left));
		layout.AddChild(CreateLabel("Coins: " + FormatChange(result.CoinsChanged), 18, HorizontalAlignment.Left));
		layout.AddChild(CreateLabel("Points: " + FormatChange(result.PointsChanged), 18, HorizontalAlignment.Left));
		layout.AddChild(CreateLabel("Updated balance: " + result.TotalCoins + " coins, " + result.TotalPoints + " points", 18, HorizontalAlignment.Left));

		var returnButton = new Button
		{
			Text = "Return to Map",
			CustomMinimumSize = new Vector2(0, 46)
		};
		returnButton.Pressed += ReturnToMap;
		layout.AddChild(returnButton);
	}

	private void ReturnToMap()
	{
		countdown?.Stop();
		Hide();
		MiniGameLauncher.CompleteChallenge();
	}

	private void SetAnswerControlsDisabled(bool disabled)
	{
		if (submitButton != null)
			submitButton.Disabled = disabled;
		if (answerInput != null)
			answerInput.Editable = !disabled;
		foreach (Button button in answerButtons)
			button.Disabled = disabled;
	}

	private void UpdateTimerLabel()
	{
		if (timerLabel != null)
			timerLabel.Text = "Time remaining: " + timeLeft + " seconds";
	}

	private static Label CreateLabel(string text, int fontSize, HorizontalAlignment alignment)
	{
		var label = new Label
		{
			Text = text,
			HorizontalAlignment = alignment,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.Word
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		return label;
	}

	private static string FormatChange(int value)
	{
		return value > 0 ? "+" + value : value.ToString();
	}

	private void RemoveExistingControls()
	{
		foreach (Node child in GetChildren())
		{
			RemoveChild(child);
			child.QueueFree();
		}
	}
}
