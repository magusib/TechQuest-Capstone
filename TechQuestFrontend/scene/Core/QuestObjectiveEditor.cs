#nullable enable
using Godot;
using System;

public partial class QuestObjectiveEditor : VBoxContainer
{
	private readonly System.Collections.Generic.List<MiniGameDto> miniGames = new();
	private OptionButton objectiveType = null!;
	private SpinBox targetQuantity = null!;
	private OptionButton miniGameType = null!;
	private CheckBox requireMiniGameSuccess = null!;

	public override void _Ready()
	{
		objectiveType = GetNode<OptionButton>("ObjectiveRow/ObjectiveType");
		targetQuantity = GetNode<SpinBox>("TargetQuantityRow/TargetQuantity");
		miniGameType = GetNode<OptionButton>("MiniGameRow/MiniGameType");
		requireMiniGameSuccess = GetNode<CheckBox>("RequireMiniGameSuccess");

		if (objectiveType.ItemCount == 0)
		{
			objectiveType.AddItem("Manual");
			objectiveType.AddItem("Complete NPC challenges");
			objectiveType.AddItem("Play mini-games");
			objectiveType.AddItem("Receive coins");
			objectiveType.AddItem("Receive points");
		}

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

	private void UpdateVisibility()
	{
		int selected = objectiveType.Selected;
		GetNode<Control>("MiniGameRow").Visible = selected == 2;
		requireMiniGameSuccess.Visible = selected == 2;
	}
}
