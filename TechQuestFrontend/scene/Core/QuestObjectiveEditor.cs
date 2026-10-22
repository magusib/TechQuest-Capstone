#nullable enable
using Godot;
using System;

public partial class QuestObjectiveEditor : VBoxContainer
{
	private readonly System.Collections.Generic.List<MiniGameDto> miniGames = new();
	private readonly OptionButton objectiveType = new();
	private readonly SpinBox targetQuantity = new();
	private readonly OptionButton miniGameType = new();
	private readonly CheckBox requireMiniGameSuccess = new();

	public QuestObjectiveEditor()
	{
		Name = "QuestObjectiveEditor";
		SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		AddThemeConstantOverride("separation", 2);

		objectiveType.AddItem("Manual");
		objectiveType.AddItem("Complete NPC challenges");
		objectiveType.AddItem("Play mini-games");
		objectiveType.AddItem("Receive coins");
		objectiveType.AddItem("Receive points");
		objectiveType.Select(0);
		AddRow("Objective", objectiveType);

		targetQuantity.MinValue = 1;
		targetQuantity.MaxValue = int.MaxValue;
		targetQuantity.Step = 1;
		targetQuantity.Value = 1;
		AddRow("Target quantity", targetQuantity);

		AddRow("Mini-game", miniGameType);

		requireMiniGameSuccess.Text = "Require a successful mini-game";
		AddChild(requireMiniGameSuccess);

		objectiveType.ItemSelected += _ => UpdateVisibility();
		UpdateVisibility();
	}

	public string ObjectiveType => objectiveType.Selected switch
	{
		1 => "npc_challenge",
		2 => "mini_game",
		3 => "coins",
		4 => "points",
		_ => "manual"
	};

	public int TargetQuantity => (int)targetQuantity.Value;

	public string? RequiredMiniGameType => miniGameType.Selected switch
	{
		var selected when selected >= 0 && selected < miniGames.Count => miniGames[selected].GameType,
		_ => null
	};

	public int? RequiredMiniGameId => miniGameType.Selected >= 0 && miniGameType.Selected < miniGames.Count
		? miniGames[miniGameType.Selected].MiniGameId
		: null;

	public bool RequireMiniGameSuccess => requireMiniGameSuccess.ButtonPressed;

	public void ResetValues()
	{
		objectiveType.Select(0);
		targetQuantity.Value = 1;
		if (miniGames.Count > 0)
			miniGameType.Select(0);
		requireMiniGameSuccess.ButtonPressed = false;
		UpdateVisibility();
	}

	public void SetMiniGames(System.Collections.Generic.List<MiniGameDto> availableGames)
	{
		miniGameType.Clear();
		miniGames.Clear();
		foreach (var game in availableGames)
		{
			if (!string.Equals(game.GameType, "FallingDuck", StringComparison.OrdinalIgnoreCase) &&
				!string.Equals(game.GameType, "MatchThePairs", StringComparison.OrdinalIgnoreCase) &&
				!string.Equals(game.GameType, "QuickTapChallenge", StringComparison.OrdinalIgnoreCase))
				continue;

			miniGames.Add(game);
			miniGameType.AddItem($"{game.GameName} ({game.GameType})");
		}

		if (miniGames.Count > 0)
			miniGameType.Select(0);
	}

	private void AddRow(string labelText, Control control)
	{
		var row = new HBoxContainer
		{
			CustomMinimumSize = new Vector2(0, 28),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		row.AddChild(new Label
		{
			Text = labelText,
			CustomMinimumSize = new Vector2(160, 0),
			VerticalAlignment = VerticalAlignment.Center
		});
		control.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		row.AddChild(control);
		AddChild(row);
	}

	private void UpdateVisibility()
	{
		int selected = objectiveType.Selected;
		((Control)GetChild(2)).Visible = selected == 2;
		requireMiniGameSuccess.Visible = selected == 2;
	}
}
