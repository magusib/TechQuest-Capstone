using Godot;
using System;
using System.Threading.Tasks;

public static class MiniGameLauncher
{
	private static bool isOpening;
	private static bool isProceeding;
	private static bool isOpeningCharacterChallenge;
	private static StudentChallengeStartDto currentChallenge;

	public static async void OpenForDeployment(Node source, int deploymentId)
	{
		if (isOpening || !UserSession.IsStudent)
			return;

		isOpening = true;
		SceneTree tree = null;
		CanvasLayer overlayLayer = null;
		try
		{
			MiniGameAssignmentDto assignment = await MiniGameApiClient.GetAssignmentAsync(deploymentId);
			if (source == null || !GodotObject.IsInstanceValid(source) || !source.IsInsideTree())
				return;

			GetScenePath(assignment.GameType);

			StudentChallengeStartDto challenge = await MiniGameApiClient.StartChallengeAsync(deploymentId);
			if (challenge.QuestionId != assignment.QuestionId ||
				challenge.DeploymentId != deploymentId)
				throw new InvalidOperationException("NPC challenge assignment changed while it was being started.");

			string scenePath = GetScenePath(challenge.GameType);
			PackedScene gameScene = GD.Load<PackedScene>(scenePath);
			if (gameScene == null)
				throw new InvalidOperationException("Mini-game scene could not be loaded: " + scenePath);

			tree = source.GetTree();
			Node currentScene = tree.CurrentScene;
			if (currentScene == null)
				throw new InvalidOperationException("The current map could not be found.");

			overlayLayer = new()
			{
				Name = "MiniGameOverlay",
				Layer = 100,
				ProcessMode = Node.ProcessModeEnum.Always
			};
			currentScene.AddChild(overlayLayer);

			Node game = gameScene.Instantiate();
			game.ProcessMode = Node.ProcessModeEnum.Always;
			SetNestedCanvasLayersAbove(game, overlayLayer.Layer + 1);
			currentChallenge = challenge;
			int assignedDeploymentId = challenge.DeploymentId;
			int assignedNpcId = challenge.NpcId;
			if (game is MatchThePairs matchGame)
				matchGame.Configure(assignedDeploymentId, assignedNpcId, challenge.QuestionId, challenge.GameType, challenge.TimerSeconds, challenge.AttemptId);
			else if (game is FallingDuck duckGame)
				duckGame.Configure(assignedDeploymentId, assignedNpcId, challenge.QuestionId, challenge.GameType, challenge.TimerSeconds, challenge.AttemptId);
			else if (game is QuickTapChallenge tapGame)
				tapGame.Configure(assignedDeploymentId, assignedNpcId, challenge.QuestionId, challenge.GameType, challenge.TimerSeconds, challenge.AttemptId);
			else
				throw new InvalidOperationException("The selected mini-game scene has an unsupported script.");

			overlayLayer.AddChild(game);
			if (game is MatchThePairs configuredMatchGame)
				configuredMatchGame.StartAssignedGame();
			else if (game is FallingDuck configuredDuckGame)
				configuredDuckGame.StartAssignedGame();
			else if (game is QuickTapChallenge configuredTapGame)
				configuredTapGame.StartAssignedGame();

			tree.Paused = true;
		}
		catch (Exception exception)
		{
			if (overlayLayer != null && GodotObject.IsInstanceValid(overlayLayer))
				overlayLayer.QueueFree();
			if (tree != null)
				tree.Paused = false;
			GD.PrintErr("NPC mini-game could not be opened: " + exception.Message);
			ShowMessage(source, exception.Message);
		}
		finally
		{
			isOpening = false;
		}
	}

	public static async void OpenCharacterChallenge(Node source, int deploymentId)
	{
		if (isOpeningCharacterChallenge || !UserSession.IsStudent ||
			source == null || !GodotObject.IsInstanceValid(source) || !source.IsInsideTree())
			return;

		isOpeningCharacterChallenge = true;
		SceneTree tree = source.GetTree();
		CanvasLayer overlayLayer = null;
		try
		{
			string characterPath = await SelectedCharacterService.GetOwnedSelectedScenePathAsync();
			string characterName = System.IO.Path.GetFileNameWithoutExtension(characterPath);
			if (string.IsNullOrWhiteSpace(characterName))
				characterName = "Character";

			CharacterChallengeStartDto challenge =
				await MiniGameApiClient.StartCharacterChallengeAsync(deploymentId, characterName);
			if (challenge.State == "CharacterMiniGameCompleted")
				throw new InvalidOperationException(
					"This NPC character challenge has already been completed. Challenge another NPC.");
			PackedScene gameScene = GD.Load<PackedScene>(
				"res://scene/Minigame/NPCMinigameChallange/HigherLower.tscn");
			if (gameScene == null)
				throw new InvalidOperationException("The HigherLower scene could not be loaded.");

			Node currentScene = tree.CurrentScene;
			if (currentScene == null)
				throw new InvalidOperationException("The current map could not be found.");

			overlayLayer = new CanvasLayer
			{
				Name = "HigherLowerOverlay",
				Layer = 100,
				ProcessMode = Node.ProcessModeEnum.Always
			};
			currentScene.AddChild(overlayLayer);

			Node game = gameScene.Instantiate();
			game.ProcessMode = Node.ProcessModeEnum.Always;
			SetNestedCanvasLayersAbove(game, overlayLayer.Layer + 1);
			overlayLayer.AddChild(game);
			if (game is HigherLower higherLower)
				higherLower.Configure(challenge, source);
			else
				throw new InvalidOperationException("HigherLower.tscn must use the HigherLower script.");

			tree.Paused = true;
		}
		catch (Exception exception)
		{
			if (overlayLayer != null && GodotObject.IsInstanceValid(overlayLayer))
				overlayLayer.QueueFree();
			tree.Paused = false;
			GD.PrintErr("Character challenge could not be opened: " + exception.Message);
			ShowMessage(source, exception.Message);
		}
		finally
		{
			isOpeningCharacterChallenge = false;
		}
	}

	public static async void ProceedToQuestion(Node game, int deploymentId)
	{
		if (isProceeding || currentChallenge == null ||
			game == null || !GodotObject.IsInstanceValid(game) || !game.IsInsideTree())
			return;

		isProceeding = true;
		try
		{
			StudentChallengeQuestionDto question =
				await MiniGameApiClient.GetStudentQuestionAsync(deploymentId);
			Node currentScene = game.GetTree().CurrentScene;
			string sceneName = question.Difficulty + "MapQuestion";
			NpcQuestionFlow questionFlow = currentScene?.FindChild(sceneName, true, false) as NpcQuestionFlow;
			if (questionFlow == null)
				throw new InvalidOperationException("The question screen for this map could not be found.");

			await questionFlow.ShowQuestionAsync(question);
			CloseOverlay(game);
		}
		catch (Exception exception)
		{
			GD.PrintErr("Assigned question could not be opened: " + exception.Message);
			ShowMessage(game, exception.Message);
		}
		finally
		{
			isProceeding = false;
		}
	}

	public static void ResumeQuestionFromMap(Node source, int deploymentId, string answerTip)
	{
		if (isProceeding ||
			source == null ||
			!GodotObject.IsInstanceValid(source) ||
			!source.IsInsideTree())
			return;

		Node currentScene = source.GetTree().CurrentScene;
		NpcQuestionFlow questionFlow = currentScene?.FindChild("EasyMapQuestion", true, false) as NpcQuestionFlow
			?? currentScene?.FindChild("MediumMapQuestion", true, false) as NpcQuestionFlow
			?? currentScene?.FindChild("HardMapQuestion", true, false) as NpcQuestionFlow;
		if (currentScene == null || questionFlow == null)
		{
			ShowMessage(source, "The question screen for this map could not be found.");
			return;
		}

		var overlayLayer = new CanvasLayer
		{
			Name = "NpcAnswerTipOverlay",
			Layer = 100,
			ProcessMode = Node.ProcessModeEnum.Always
		};
		currentScene.AddChild(overlayLayer);

		var root = new Control { ProcessMode = Node.ProcessModeEnum.Always };
		root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		overlayLayer.AddChild(root);
		var backdrop = new ColorRect
		{
			Color = new Color(0, 0, 0, 0.72f),
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(backdrop);

		var panel = new PanelContainer { CustomMinimumSize = new Vector2(620, 300) };
		panel.SetAnchorsPreset(Control.LayoutPreset.Center);
		panel.OffsetLeft = -310;
		panel.OffsetTop = -150;
		panel.OffsetRight = 310;
		panel.OffsetBottom = 150;
		root.AddChild(panel);

		var layout = new VBoxContainer();
		layout.AddThemeConstantOverride("separation", 20);
		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 28);
		margin.AddThemeConstantOverride("margin_top", 24);
		margin.AddThemeConstantOverride("margin_right", 28);
		margin.AddThemeConstantOverride("margin_bottom", 24);
		margin.AddChild(layout);
		panel.AddChild(margin);

		var tipLabel = new Label
		{
			Text = string.IsNullOrWhiteSpace(answerTip)
				? "No answer tip has been configured for this NPC."
				: "Answer tip: " + answerTip,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.Word,
			CustomMinimumSize = new Vector2(0, 120)
		};
		tipLabel.AddThemeFontSizeOverride("font_size", 22);
		layout.AddChild(tipLabel);

		var statusLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.Word
		};
		layout.AddChild(statusLabel);

		var proceedButton = new Button
		{
			Text = "Proceed to Question",
			CustomMinimumSize = new Vector2(0, 46)
		};
		layout.AddChild(proceedButton);
		proceedButton.Pressed += async () =>
		{
			if (isProceeding)
				return;

			isProceeding = true;
			proceedButton.Disabled = true;
			statusLabel.Text = "Loading your question...";
			try
			{
				StudentChallengeQuestionDto question =
					await MiniGameApiClient.GetStudentQuestionAsync(deploymentId);
				await questionFlow.ShowQuestionAsync(question);
				overlayLayer.QueueFree();
			}
			catch (Exception exception)
			{
				GD.PrintErr("NPC question could not be resumed: " + exception.Message);
				statusLabel.Text = "The question could not be opened. " + exception.Message;
				proceedButton.Disabled = false;
			}
			finally
			{
				isProceeding = false;
			}
		};
	}

	public static void CompleteChallenge()
	{
		currentChallenge = null;
	}

	public static void CloseOverlay(Node game)
	{
		if (game == null || !GodotObject.IsInstanceValid(game))
			return;

		SceneTree tree = game.GetTree();
		if (game is MatchThePairs matchGame)
			matchGame.StopGame();
		else if (game is FallingDuck duckGame)
			duckGame.StopGame();
		else if (game is QuickTapChallenge tapGame)
			tapGame.StopGame();

		Node overlay = game.GetParent();
		tree.Paused = false;
		if (overlay != null)
			overlay.QueueFree();
	}

	private static string GetScenePath(string gameType)
	{
		if (gameType == "MatchThePairs")
			return "res://scene/Minigame/MatchThePairs.tscn";
		if (gameType == "FallingDuck")
			return "res://scene/Minigame/FallingDuck.tscn";
		if (gameType == "QuickTapChallenge")
			return "res://scene/Minigame/QuickTapChallenge.tscn";

		throw new InvalidOperationException("The NPC has an unsupported mini-game assignment.");
	}

	private static void SetNestedCanvasLayersAbove(Node node, int minimumLayer)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is CanvasLayer canvasLayer)
				canvasLayer.Layer = Math.Max(canvasLayer.Layer, minimumLayer);

			SetNestedCanvasLayersAbove(child, minimumLayer);
		}
	}

	private static void ShowMessage(Node source, string message)
	{
		if (source == null || !GodotObject.IsInstanceValid(source) || !source.IsInsideTree())
			return;

		AcceptDialog dialog = new()
		{
			Title = "Mini-game unavailable",
			DialogText = message,
			ProcessMode = Node.ProcessModeEnum.Always
		};
		source.GetTree().CurrentScene.AddChild(dialog);
		dialog.Confirmed += dialog.QueueFree;
		dialog.PopupCentered();
	}
}
