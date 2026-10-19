using Godot;
using System;
using System.Threading.Tasks;

public static class MiniGameLauncher
{
	private static bool isOpening;

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

			string scenePath = GetScenePath(assignment.GameType);
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

			Control game = gameScene.Instantiate<Control>();
			game.ProcessMode = Node.ProcessModeEnum.Always;
			overlayLayer.AddChild(game);
			int assignedDeploymentId = assignment.DeploymentId;
			int assignedNpcId = assignment.NpcId ?? 0;
			if (game is MatchThePairs matchGame)
				matchGame.Configure(assignedDeploymentId, assignedNpcId, assignment.QuestionId, assignment.GameType);
			else if (game is FallingDuck duckGame)
				duckGame.Configure(assignedDeploymentId, assignedNpcId, assignment.QuestionId, assignment.GameType);
			else if (game is QuickTapChallenge tapGame)
				tapGame.Configure(assignedDeploymentId, assignedNpcId, assignment.QuestionId, assignment.GameType);
			else
				throw new InvalidOperationException("The selected mini-game scene has an unsupported script.");
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
