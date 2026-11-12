using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public abstract partial class NpcQuestionFlow : Control
{
    private StudentChallengeQuestionDto question;
    private Panel questionPanel;
    private Label subjectLabel;
    private Label timerLabel;
    private Label questionLabel;
    private LineEdit answerInput;
    private Button submitButton;
    private readonly List<Button> choiceButtons = new();
    private Label hintAnswerLabel;
    private Control purchaseOverlay;
    private Label purchaseTitleLabel;
    private Label notificationLabel;
    private Button extendTimeButton;
    private Button extendTimeHintButton;
    private Button getHintsButton;
    private Button noButton;
    private Control questionResult;
    private Panel timeReachPanel;
    private Panel wrongAnswerPanel;
    private Panel correctAnswerPanel;
    private Timer countdown;
    private int timeLeft;
    private bool isSubmitting;
    private bool isFinalized;
    private bool purchaseInProgress;
    private string selectedAnswer = "";

    protected abstract string Difficulty { get; }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        questionPanel = FindChild(Difficulty + "MapQuestionPanel", true, false) as Panel;
        subjectLabel = questionPanel.GetNode<Label>("SubjectLabel");
        timerLabel = questionPanel.GetNode<Label>("TimerLabel");
        questionLabel = questionPanel.GetNode<Panel>("QuestionPanel").GetNode<Label>("QuestionLabel");
        answerInput = questionPanel.GetNode<LineEdit>("LineEdit");
        submitButton = questionPanel.GetNode<Button>("Button");
        choiceButtons.Add(questionPanel.GetNode<Button>("ChoiceAButton"));
        choiceButtons.Add(questionPanel.GetNode<Button>("ChoiceBButton"));
        choiceButtons.Add(questionPanel.GetNode<Button>("ChoiceCButton"));
        choiceButtons.Add(questionPanel.GetNode<Button>("ChoiceDButton"));

        hintAnswerLabel = GetNode<Label>("HintAnswerLabel");
        hintAnswerLabel.ZIndex = 20;
        hintAnswerLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        purchaseOverlay = GetNode<Control>("PurchaseOverlay");
        purchaseTitleLabel = GetNode<Label>("PurchaseOverlay/PurchasePanel/VBoxContainer/TitleLabel");
        notificationLabel = GetNode<Label>("PurchaseOverlay/PurchasePanel/VBoxContainer/NotificationLabel");
        extendTimeButton = GetNode<Button>("PurchaseOverlay/PurchasePanel/VBoxContainer/ExtendTimeButton");
        extendTimeHintButton = GetNode<Button>("PurchaseOverlay/PurchasePanel/VBoxContainer/ExtendTimeHintButton");
        getHintsButton = GetNode<Button>("PurchaseOverlay/PurchasePanel/VBoxContainer/GetHintsButton");
        noButton = GetNode<Button>("PurchaseOverlay/PurchasePanel/VBoxContainer/NoButton");

        questionResult = GetNode<Control>("QuestionResult");
        timeReachPanel = GetNode<Panel>("QuestionResult/TimeReachPanel");
        wrongAnswerPanel = GetNode<Panel>("QuestionResult/WrongAnswerPanel");
        correctAnswerPanel = GetNode<Panel>("QuestionResult/CorrectAnswerPanel");

        submitButton.Pressed += SubmitSelectedAnswer;
        for (int index = 0; index < choiceButtons.Count; index++)
        {
            int capturedIndex = index;
            choiceButtons[index].Pressed += async () => await SubmitAnswerAsync(choiceButtons[capturedIndex].Text, false);
        }
        extendTimeButton.Pressed += () => PurchaseOptionAsync("extend_time");
        extendTimeHintButton.Pressed += () => PurchaseOptionAsync("extend_time_hint");
        getHintsButton.Pressed += () => PurchaseOptionAsync("hint");
        noButton.Pressed += FinalizeWithoutAnswerAsync;

        HideAllRuntimePanels();
        Hide();
    }

    public async Task ShowQuestionAsync(StudentChallengeQuestionDto assignedQuestion)
    {
        if (assignedQuestion == null || assignedQuestion.DeploymentId <= 0 ||
            !string.Equals(assignedQuestion.Difficulty, Difficulty, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The NPC question configuration is incomplete or does not match this map.");

        question = assignedQuestion;
        timeLeft = Math.Max(0, question.RemainingSeconds);
        selectedAnswer = "";
        isSubmitting = false;
        isFinalized = false;
        purchaseInProgress = false;
        subjectLabel.Text = question.SubjectName;
        questionLabel.Text = question.Question;
        answerInput.Text = "";
        hintAnswerLabel.Text = "";
        hintAnswerLabel.Hide();
        questionPanel.Show();
        questionResult.Hide();
        purchaseOverlay.Hide();
        ConfigureAnswerControls();
        UpdateTimerLabel();
        Show();

        countdown?.QueueFree();
        countdown = new Timer { WaitTime = 1, OneShot = false, ProcessMode = ProcessModeEnum.Always };
        countdown.Timeout += OnCountdownTimeout;
        AddChild(countdown);
        if (timeLeft == 0)
            ShowPurchaseOptions(true);
        else
            countdown.Start();
        await Task.CompletedTask;
    }

    private void ConfigureAnswerControls()
    {
        bool hasChoices = question.AnswerChoices != null && question.AnswerChoices.Count > 0;
        answerInput.Visible = !hasChoices;
        for (int index = 0; index < choiceButtons.Count; index++)
        {
            bool visible = hasChoices && index < question.AnswerChoices.Count;
            choiceButtons[index].Visible = visible;
            if (visible)
                choiceButtons[index].Text = question.AnswerChoices[index];
        }
        SetAnswerControlsDisabled(false);
    }

    private void OnCountdownTimeout()
    {
        if (isSubmitting || isFinalized || purchaseOverlay.Visible)
            return;
        timeLeft--;
        UpdateTimerLabel();
        if (timeLeft <= 0)
        {
            countdown.Stop();
            SetAnswerControlsDisabled(true);
            ShowPurchaseOptions(true);
        }
    }

    private async void SubmitSelectedAnswer()
    {
        await SubmitAnswerAsync(answerInput.Text.Trim(), false);
    }

    private async Task SubmitAnswerAsync(string answer, bool finalize)
    {
        if (isSubmitting || isFinalized || purchaseInProgress)
            return;
        if (!finalize && timeLeft > 0 && string.IsNullOrWhiteSpace(answer))
            return;

        isSubmitting = true;
        selectedAnswer = answer;
        countdown?.Stop();
        SetAnswerControlsDisabled(true);
        try
        {
            QuestionAttemptResultDto result = await MiniGameApiClient.SubmitQuestionAttemptAsync(
                question.DeploymentId, answer, finalize);
            if (!result.Completed)
            {
                ShowPurchaseOptions(false);
                return;
            }

            isFinalized = true;
            UserSession.UpdateStudentProfile(result.TotalCoins, result.TotalPoints);
            try
            {
                LifespanResponseDto lifespan = await LifespanApiClient.GetAsync();
                UserSession.ApplyLifespanResponse(lifespan);
            }
            catch (Exception exception)
            {
                GD.PrintErr("Could not refresh lifespan after quiz: " + exception.Message);
            }
            NpcMapSprite.RemoveDeploymentSprite(GetTree().CurrentScene, question.DeploymentId);
            ShowResult(result);
        }
        catch (Exception exception)
        {
            notificationLabel.Text = exception.Message;
            ShowPurchaseOptions(false);
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private async void FinalizeWithoutAnswerAsync()
    {
        if (purchaseInProgress || isFinalized)
            return;
        purchaseOverlay.Hide();
        await SubmitAnswerAsync("", true);
    }

    private async void PurchaseOptionAsync(string purchaseType)
    {
        if (purchaseInProgress || isFinalized)
            return;
        purchaseInProgress = true;
        SetPurchaseButtonsDisabled(true);
        notificationLabel.Text = "Processing purchase...";
        try
        {
            NpcQuizPurchaseResultDto result = await MiniGameApiClient.PurchaseNpcQuizOptionAsync(
                question.DeploymentId, Guid.NewGuid(), purchaseType);
            UserSession.UpdateStudentProfile(result.TotalCoins, UserSession.Points);
            timeLeft = Math.Max(0, result.TimeExtensionSeconds);
            UpdateTimerLabel();
            if (!string.IsNullOrWhiteSpace(result.HintDisplay))
            {
                hintAnswerLabel.Text = result.HintDisplay.Trim();
                hintAnswerLabel.Show();
                hintAnswerLabel.MoveToFront();
            }
            purchaseOverlay.Hide();
            SetAnswerControlsDisabled(false);
            countdown.Start();
        }
        catch (Exception exception)
        {
            notificationLabel.Text = exception.Message;
            SetPurchaseButtonsDisabled(false);
        }
        finally
        {
            purchaseInProgress = false;
        }
    }

    private void ShowPurchaseOptions(bool timeExpired)
    {
        purchaseTitleLabel.Text = timeExpired ? "Time is up. Continue the question?" : "Incorrect answer. Need help?";
        extendTimeButton.Visible = timeExpired;
        extendTimeHintButton.Visible = timeExpired;
        getHintsButton.Visible = !timeExpired;
        notificationLabel.Text = "";
        SetPurchaseButtonsDisabled(false);
        purchaseOverlay.Show();
        purchaseOverlay.MoveToFront();
    }

    private void ShowResult(QuestionAttemptResultDto result)
    {
        HideAllRuntimePanels();
        questionPanel.Hide();
        questionResult.Show();
        Panel panel;
        if (result.IsCorrect)
        {
            panel = correctAnswerPanel;
            panel.GetNode<Label>("CorrectAnswerLabel").Text = "Correct answer: " + result.CorrectAnswer;
            panel.GetNode<Label>("CoinsReceivedLabel").Text = "Coins: " + FormatChange(result.CoinsChanged);
            panel.GetNode<Label>("PointsReceiveLabel").Text =
                "Points: " + FormatChange(result.PointsChanged) + "\nLifespan deducted: 0";
        }
        else if (result.TimedOut)
        {
            panel = timeReachPanel;
            panel.GetNode<Label>("CorrectAnswerLabel").Text = "Correct answer: " + result.CorrectAnswer;
            panel.GetNode<Label>("CoinsDeductedLabel").Text = "Coins: " + FormatChange(result.CoinsChanged);
            panel.GetNode<Label>("PointsDeductedLabel").Text =
                "Points: " + FormatChange(result.PointsChanged) + "\nLifespan deducted: 10";
        }
        else
        {
            panel = wrongAnswerPanel;
            panel.GetNode<Label>("CorrectAnswerLabel").Text = "Correct answer: " + result.CorrectAnswer;
            panel.GetNode<Label>("CoinsDeductedLabel").Text = "Coins: " + FormatChange(result.CoinsChanged);
            panel.GetNode<Label>("PointsDeductedLabel").Text =
                "Points: " + FormatChange(result.PointsChanged) + "\nLifespan deducted: 10";
        }
        panel.Show();
        foreach (Node node in panel.GetChildren())
        {
            if (node is Button button)
                button.Pressed += ReturnToMap;
        }
    }

    private void HideAllRuntimePanels()
    {
        questionPanel?.Hide();
        purchaseOverlay?.Hide();
        questionResult?.Hide();
        timeReachPanel?.Hide();
        wrongAnswerPanel?.Hide();
        correctAnswerPanel?.Hide();
        hintAnswerLabel?.Hide();
    }

    private void SetAnswerControlsDisabled(bool disabled)
    {
        submitButton.Disabled = disabled;
        answerInput.Editable = !disabled;
        foreach (Button button in choiceButtons)
            button.Disabled = disabled;
    }

    private void SetPurchaseButtonsDisabled(bool disabled)
    {
        extendTimeButton.Disabled = disabled;
        extendTimeHintButton.Disabled = disabled;
        getHintsButton.Disabled = disabled;
        noButton.Disabled = disabled;
    }

    private void UpdateTimerLabel()
    {
        timerLabel.Text = "Time: " + Math.Max(0, timeLeft).ToString("00");
    }

    private void ReturnToMap()
    {
        countdown?.Stop();
        answerInput?.ReleaseFocus();
        Hide();
        MiniGameLauncher.CompleteChallenge();
    }

    private static string FormatChange(int value) => value > 0 ? "+" + value : value.ToString();
}
