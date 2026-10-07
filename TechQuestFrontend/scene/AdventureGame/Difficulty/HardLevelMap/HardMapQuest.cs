using Godot;
using System;
using System.Collections.Generic;

public partial class HardMapQuest : Control
{
	private const string DifficultyName = "Hard";
	private QuestListDto currentQuest;
	private VBoxContainer questListContainer;
	private Control questItemTemplate;
	private Control selectedQuestItem;
	private Button claimButton;
	private Button closeButton;
	private Button infoCloseButton;
	private Button rewardOkButton;
	private Panel rewardPopup;
	private Panel questInfoPanel;
	private Control studentQuestPopup;
	private Control adminQuestAdd;
	private Control adminQuestAddPanel;
	private Control adminQuestItemTemplate;
	private VBoxContainer adminQuestListContainer;
	private Button adminAddButton;
	private Button adminAddLabelButton;
	private Button adminSaveButton;
	private Button adminFormCloseButton;
	private Button adminPopupCloseButton;
	private LineEdit adminQuestTitleInput;
	private LineEdit adminQuestDescriptionInput;
	private LineEdit adminQuestCoinsInput;
	private LineEdit adminQuestPointsInput;
	private Label adminFormMessageLabel;
	private Label questTitleLabel;
	private Label questDescriptionLabel;
	private Label questStatusLabel;
	private Label rewardCoinsLabel;
	private Label rewardPointsLabel;
	private Label rewardPopupCoinsLabel;
	private Label rewardPopupPointsLabel;
	private Label rewardPopupLabel;
	private Label resetLabel;
	private DateTime? resetAtUtc;
	private double resetUpdateTimer;

	public override void _Ready()
	{
		MouseFilter = Control.MouseFilterEnum.Ignore;
		studentQuestPopup = GetNode<Control>("StudentQuestPopup");
		adminQuestAdd = GetNode<Control>("AdminQuestAdd");
		adminQuestAdd.MouseFilter = Control.MouseFilterEnum.Ignore;
		questInfoPanel = GetNode<Panel>("StudentQuestPopup/QuestPanel/QuestInfoPanel");
		studentQuestPopup.Hide();
		adminQuestAdd.Hide();
		questItemTemplate = GetNode<Control>("StudentQuestPopup/QuestPanel/QuestListPanel/QuestItem");
		questItemTemplate.Hide();
		adminQuestAddPanel = adminQuestAdd.FindChild("QuestAddPanel", true, false) as Control;
		adminQuestItemTemplate = adminQuestAdd.FindChild("QuestItem", true, false) as Control;
		adminQuestItemTemplate.Hide();

		ScrollContainer adminQuestScroll = adminQuestAdd.GetNode<ScrollContainer>("QuestPanel/QuestListPanel/ScrollContainer");
		adminQuestListContainer = new VBoxContainer();
		adminQuestListContainer.Name = "AdminQuestListContainer";
		adminQuestListContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		adminQuestListContainer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		adminQuestListContainer.AddThemeConstantOverride("separation", 10);
		adminQuestScroll.AddChild(adminQuestListContainer);

		adminAddButton = adminQuestAdd.FindChild("AddButton", true, false) as Button;
		if (adminAddButton == null)
			adminAddButton = adminQuestAdd.FindChild("AddQuest", true, false) as Button;
		adminAddLabelButton = adminQuestAdd.FindChild("ResetLabel", true, false) as Button;
		adminSaveButton = adminQuestAdd.FindChild("SaveButton", true, false) as Button;
		adminFormCloseButton = adminQuestAddPanel.FindChild("CloseButton", true, false) as Button;
		adminPopupCloseButton = adminQuestAdd.GetNodeOrNull<Button>("QuestPanel/CloseButton");
		adminQuestTitleInput = adminQuestAdd.FindChild("QuestTitle", true, false) as LineEdit;
		adminQuestDescriptionInput = adminQuestAdd.FindChild("Description", true, false) as LineEdit;
		adminQuestCoinsInput = adminQuestAdd.FindChild("CoinsReward", true, false) as LineEdit;
		adminQuestPointsInput = adminQuestAdd.FindChild("PointsReward", true, false) as LineEdit;
		adminFormMessageLabel = adminQuestAdd.FindChild("AddQuestLabel", true, false) as Label;

		questListContainer = new VBoxContainer();
		questListContainer.Name = "QuestListContainer";
		questListContainer.CustomMinimumSize = new Vector2(340, 0);
		questListContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		questListContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		questListContainer.AddThemeConstantOverride("separation", 10);

		var scroll = GetNode<ScrollContainer>("StudentQuestPopup/QuestPanel/QuestListPanel/ScrollContainer");
		scroll.AddChild(questListContainer);

		claimButton = GetNode<Button>("StudentQuestPopup/QuestPanel/QuestInfoPanel/ClaimButton");
		closeButton = GetNode<Button>("StudentQuestPopup/QuestPanel/CloseButton");
		rewardPopup = GetNode<Panel>("StudentQuestPopup/QuestPanel/RewardPopup");
		rewardOkButton = GetNode<Button>("StudentQuestPopup/QuestPanel/RewardPopup/Button");
		rewardPopupLabel = GetNode<Label>("StudentQuestPopup/QuestPanel/RewardPopup/RewardLabel");
		rewardPopupCoinsLabel = GetNode<Label>("StudentQuestPopup/QuestPanel/RewardPopup/RewardContainer/CoinsPanel/CoinsLabel");
		rewardPopupPointsLabel = GetNode<Label>("StudentQuestPopup/QuestPanel/RewardPopup/RewardContainer/PointsPanel/PointsLabel");
		questTitleLabel = GetNode<Label>("StudentQuestPopup/QuestPanel/QuestInfoPanel/Panel/QuestTitleLabel");
		questDescriptionLabel = GetNode<Label>("StudentQuestPopup/QuestPanel/QuestInfoPanel/Panel/QuestDescriptionLabel");
		questStatusLabel = GetNode<Label>("StudentQuestPopup/QuestPanel/QuestInfoPanel/HBoxContainer/CompleteLabel");
		rewardCoinsLabel = GetNode<Label>("StudentQuestPopup/QuestPanel/QuestInfoPanel/RewardContainer/CoinsPanel/CoinsLabel");
		rewardPointsLabel = GetNode<Label>("StudentQuestPopup/QuestPanel/QuestInfoPanel/RewardContainer/PointsPanel/PointsLabel");
		infoCloseButton = GetNode<Button>("StudentQuestPopup/QuestPanel/QuestInfoPanel/CloseButton");
		resetLabel = GetNode<Label>("StudentQuestPopup/QuestPanel/ResetLabel");

		closeButton.Pressed += OnClosePressed;
		infoCloseButton.Pressed += OnInfoClosePressed;
		adminFormCloseButton.Pressed += OnAdminClosePressed;
		adminSaveButton.Pressed += OnAdminSavePressed;
		if (adminAddButton != null)
			adminAddButton.Pressed += OnAdminAddQuestPressed;
		if (adminAddLabelButton != null)
			adminAddLabelButton.Pressed += OnAdminAddQuestPressed;
		if (adminPopupCloseButton != null)
			adminPopupCloseButton.Pressed += OnAdminPopupClosePressed;
		claimButton.Pressed += OnClaimPressed;
		rewardOkButton.Pressed += OnRewardOkPressed;
		rewardPopup.Hide();
	}

	public void OpenForStudent()
	{
		if (!UserSession.IsStudent)
			return;

		Show();
		studentQuestPopup.Show();
		adminQuestAdd.Hide();
		currentQuest = null;
		questInfoPanel.Hide();
		LoadQuests();
	}

	public override void _Process(double delta)
	{
		resetUpdateTimer += delta;
		if (resetUpdateTimer < 1.0)
			return;

		resetUpdateTimer = 0;
		UpdateResetLabel();
	}

	public void OpenAdminQuestAdd()
	{
		if (!UserSession.IsAdmin)
			return;

		Show();
		studentQuestPopup.Hide();
		adminQuestAdd.Show();
		adminQuestAddPanel.Hide();
		LoadAdminQuests();
	}

	private void OnAdminAddQuestPressed()
	{
		if (!UserSession.IsAdmin)
			return;

		adminFormMessageLabel.Text = "Add New Quest";
		adminQuestAddPanel.Show();
	}

	private void OnAdminClosePressed()
	{
		adminQuestAddPanel.Hide();
		adminFormMessageLabel.Text = "Add New Quest";
	}

	private void OnAdminPopupClosePressed()
	{
		adminQuestAdd.Hide();
		adminQuestAddPanel.Hide();
		Hide();
	}

	private void OnAdminSavePressed()
	{
		string title = adminQuestTitleInput.Text.Trim();
		string description = adminQuestDescriptionInput.Text.Trim();
		if (string.IsNullOrWhiteSpace(title))
		{
			adminFormMessageLabel.Text = "Quest title is required.";
			return;
		}

		if (!int.TryParse(adminQuestCoinsInput.Text, out int rewardCoins) || rewardCoins < 0)
		{
			adminFormMessageLabel.Text = "Enter a valid non-negative coin reward.";
			return;
		}

		if (!int.TryParse(adminQuestPointsInput.Text, out int rewardPoints) || rewardPoints < 0)
		{
			adminFormMessageLabel.Text = "Enter a valid non-negative points reward.";
			return;
		}

		try
		{
			QuestApiClient.CreateQuest(title, description, rewardCoins, rewardPoints, DifficultyName);
			adminQuestTitleInput.Clear();
			adminQuestDescriptionInput.Clear();
			adminQuestCoinsInput.Clear();
			adminQuestPointsInput.Clear();
			OnAdminClosePressed();
			LoadAdminQuests();
		}
		catch (Exception ex)
		{
			GD.PrintErr(ex.Message);
			adminFormMessageLabel.Text = "Could not save quest.";
		}
	}

	private void LoadAdminQuests()
	{
		foreach (Node child in adminQuestListContainer.GetChildren())
			child.QueueFree();

		try
		{
			var quests = QuestApiClient.GetAvailableQuestsForDifficulty(DifficultyName);
			if (quests.Count == 0)
			{
				var emptyLabel = new Label();
				emptyLabel.Text = "No quests created yet.";
				adminQuestListContainer.AddChild(emptyLabel);
				return;
			}

			foreach (var quest in quests)
			{
				Control item = (Control)adminQuestItemTemplate.Duplicate();
				item.Name = "AdminQuestItem_" + quest.QuestId;
				item.Visible = true;
				item.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
				item.CustomMinimumSize = new Vector2(0, adminQuestItemTemplate.Size.Y);
				item.GetNode<Label>("Panel/QuestTitleLabel").Text = quest.QuestTitle;
				item.GetNode<Label>("Panel/QuestDescriptionLabel").Text = quest.Description;
				adminQuestListContainer.AddChild(item);
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr(ex.Message);
			var errorLabel = new Label();
			errorLabel.Text = "Unable to load quests.";
			adminQuestListContainer.AddChild(errorLabel);
		}
	}

	private void LoadQuests()
	{
		foreach (Node child in questListContainer.GetChildren())
			child.QueueFree();

		try
		{
			var quests = QuestApiClient.GetAvailableQuestsForDifficulty(DifficultyName);
			resetAtUtc = null;
			if (quests.Count == 0)
			{
				UpdateResetLabel();
				ShowQuestMessage("No quest available.");
				return;
			}

			foreach (var quest in quests)
			{
				if (quest.EndDate.HasValue)
				{
					DateTime endDate = DateTime.SpecifyKind(quest.EndDate.Value, DateTimeKind.Utc);
					if (!resetAtUtc.HasValue || endDate < resetAtUtc.Value)
						resetAtUtc = endDate;
				}

				Control item = (Control)questItemTemplate.Duplicate();
				item.Name = "QuestItem_" + quest.QuestId;
				item.Visible = true;
				item.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
				item.CustomMinimumSize = new Vector2(0, questItemTemplate.Size.Y);
				item.GetNode<Label>("Panel/QuestTitleLabel").Text = quest.QuestTitle;
				item.GetNode<Label>("Panel/QuestDescriptionLabel").Text = "";
				item.GetNode<Label>("HBoxContainer/QuestProgressLabel").Text = "";
				item.GetNode<Label>("HBoxContainer/CompleteLabel").Text = GetQuestStatusText(quest);

				var itemButton = new Button();
				itemButton.Flat = true;
				itemButton.FocusMode = Control.FocusModeEnum.None;
				itemButton.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
				itemButton.Pressed += () => SelectQuest(quest);
				item.AddChild(itemButton);
				questListContainer.AddChild(item);
			}

			UpdateResetLabel();
		}
		catch (Exception ex)
		{
			GD.PrintErr(ex.Message);
			resetAtUtc = null;
			UpdateResetLabel();
			ShowQuestMessage("Unable to load quests.");
		}
	}

	private void UpdateResetLabel()
	{
		if (resetAtUtc == null)
		{
			resetLabel.Text = "Reset in - --:--:--";
			return;
		}

		TimeSpan remaining = resetAtUtc.Value - DateTime.UtcNow;
		if (remaining.TotalSeconds <= 0)
		{
			resetLabel.Text = "Reset in - 00:00:00";
			return;
		}

		int totalHours = (int)remaining.TotalHours;
		resetLabel.Text = $"Reset in - {totalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
	}

	private void ShowQuestMessage(string message)
	{
		currentQuest = null;
		questInfoPanel.Hide();
		claimButton.Disabled = true;
		claimButton.Text = "Claim Reward";

		var messageLabel = new Label();
		messageLabel.Name = "NoQuestLabel";
		messageLabel.Text = message;
		messageLabel.CustomMinimumSize = new Vector2(320, 52);
		questListContainer.AddChild(messageLabel);
	}

	private void SelectQuest(QuestListDto quest)
	{
		if (selectedQuestItem != null && GodotObject.IsInstanceValid(selectedQuestItem))
			selectedQuestItem.Modulate = Colors.White;

		currentQuest = quest;
		selectedQuestItem = questListContainer.GetNodeOrNull<Control>("QuestItem_" + quest.QuestId);
		if (selectedQuestItem != null)
			selectedQuestItem.Modulate = new Color(1.15f, 1.15f, 1.15f);

		questInfoPanel.Show();
		questTitleLabel.Text = quest.QuestTitle;
		questDescriptionLabel.Text = quest.Description;
		rewardCoinsLabel.Text = quest.RewardCoins.ToString();
		rewardPointsLabel.Text = quest.RewardPoints.ToString();

		bool isCompleted = quest.IsCompleted || string.Equals(quest.Status, "completed", StringComparison.OrdinalIgnoreCase);
		bool isRewardClaimed = quest.RewardClaimed;

		if (isCompleted)
		{
			questStatusLabel.Text = "✓ Completed";
		}
		else
		{
			questStatusLabel.Text = "In Progress";
		}

		if (isRewardClaimed)
		{
			claimButton.Disabled = true;
			claimButton.Text = "✓ Reward Claimed";
		}
		else if (isCompleted)
		{
			claimButton.Disabled = false;
			claimButton.Text = "Claim Reward";
		}
		else
		{
			claimButton.Disabled = true;
			claimButton.Text = "Claim Reward";
		}

	}

	private void OnClaimPressed()
	{
		if (currentQuest == null)
			return;

		if (!currentQuest.IsCompleted && !string.Equals(currentQuest.Status, "completed", StringComparison.OrdinalIgnoreCase))
			return;

		try
		{
			var result = QuestApiClient.ClaimReward(currentQuest.QuestId);
			UserSession.UpdateStudentProfile(result.TotalCoins, result.TotalPoints);
			rewardPopupLabel.Text = "QUEST COMPLETED!\n✓ Reward Claimed";
			rewardPopupCoinsLabel.Text = $"+{result.Rewards.Coins} Coins";
			rewardPopupPointsLabel.Text = $"+{result.Rewards.Points} Points";
			rewardPopup.Show();
			LoadQuests();
		}
		catch (Exception ex)
		{
			GD.PrintErr(ex.Message);
		}
	}

	private void OnRewardOkPressed()
	{
		rewardPopup.Hide();
		currentQuest = null;
		questInfoPanel.Hide();
		LoadQuests();
	}

	private void OnClosePressed()
	{
		Hide();
		studentQuestPopup.Hide();
	}

	private void OnInfoClosePressed()
	{
		currentQuest = null;
		questInfoPanel.Hide();
	}

	private static string GetQuestStatusText(QuestListDto quest)
	{
		if (quest.IsCompleted || string.Equals(quest.Status, "completed", StringComparison.OrdinalIgnoreCase))
			return "✓ Completed";

		return "In Progress";
	}
}
