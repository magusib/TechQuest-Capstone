using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class NpcDeploymentRenderer : Node
{
	[Export]
	public string Difficulty { get; set; } = "Easy";

	private bool isRefreshing;
	private bool refreshRequested;
	private Timer deploymentRefreshTimer;

	public override async void _Ready()
	{
		await RefreshDeploymentsAsync();
		if (!IsInsideTree())
			return;

		deploymentRefreshTimer = new Timer
		{
			WaitTime = 5,
			OneShot = false,
			ProcessMode = ProcessModeEnum.Always
		};
		deploymentRefreshTimer.Timeout += RefreshPendingDeployments;
		AddChild(deploymentRefreshTimer);
		deploymentRefreshTimer.Start();
	}

	private void RefreshPendingDeployments()
	{
		if (!IsInsideTree())
			return;

		_ = RefreshDeploymentsAsync();
	}

	public async Task RefreshDeploymentsAsync()
	{
		if (!IsInsideTree())
			return;

		if (isRefreshing)
		{
			refreshRequested = true;
			return;
		}

		isRefreshing = true;
		try
		{
			do
			{
				refreshRequested = false;
				Node currentScene = GetTree().CurrentScene;
				if (currentScene == null)
					return;

				List<NpcDeploymentDto> deployments = await NpcApiClient.GetDeploymentsAsync(Difficulty);
				bool removedStaleSprites = RemoveStaleDeploymentSprites(currentScene, deployments);
				if (removedStaleSprites)
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

				foreach (NpcDeploymentDto deployment in deployments)
				{
					if (deployment.Status == "Deployed")
						await SpawnDeploymentAsync(currentScene, deployment);
				}

				List<NpcSpawnPointDto> availableSpawnPoints = GetAvailableSpawnPoints(
					currentScene,
					deployments);
				List<NpcDeploymentDto> newlyDeployed =
					await NpcApiClient.ProcessDeploymentQueueAsync(Difficulty, availableSpawnPoints);
				foreach (NpcDeploymentDto deployment in newlyDeployed)
					await SpawnDeploymentAsync(currentScene, deployment);
			}
			while (refreshRequested && IsInsideTree());
		}
		catch (Exception exception)
		{
			GD.PrintErr($"NPC deployments for the {Difficulty} map could not be refreshed: {exception.Message}");
		}
		finally
		{
			isRefreshing = false;
		}
	}

	private static bool RemoveStaleDeploymentSprites(
		Node currentScene,
		List<NpcDeploymentDto> deployments)
	{
		var deployedIds = new HashSet<int>();
		foreach (NpcDeploymentDto deployment in deployments)
		{
			if (deployment.Status == "Deployed")
				deployedIds.Add(deployment.DeploymentId);
		}

		bool removedSprite = false;
		foreach (Node child in currentScene.GetChildren())
		{
			if (child is AnimatedSprite2D sprite &&
				sprite.HasMeta("DeploymentId") &&
				!deployedIds.Contains(sprite.GetMeta("DeploymentId").AsInt32()))
			{
				sprite.QueueFree();
				removedSprite = true;
			}
		}

		return removedSprite;
	}

	private List<NpcSpawnPointDto> GetAvailableSpawnPoints(
		Node currentScene,
		List<NpcDeploymentDto> deployments)
	{
		var occupiedMarkers = new HashSet<string>(StringComparer.Ordinal);
		foreach (NpcDeploymentDto deployment in deployments)
		{
			if (deployment.Status == "Deployed" && !string.IsNullOrWhiteSpace(deployment.PositionMarker))
				occupiedMarkers.Add(deployment.PositionMarker);
		}

		foreach (Node child in currentScene.GetChildren())
		{
			if (child is AnimatedSprite2D sprite &&
				!sprite.IsQueuedForDeletion() &&
				sprite.HasMeta("PositionMarker"))
			{
				occupiedMarkers.Add(sprite.GetMeta("PositionMarker").AsString());
			}
		}

		var availableSpawnPoints = new List<NpcSpawnPointDto>();
		foreach (Node child in GetChildren())
		{
			if (child is Marker2D marker && !occupiedMarkers.Contains(marker.Name.ToString()))
			{
				availableSpawnPoints.Add(new NpcSpawnPointDto
				{
					PositionMarker = marker.Name,
					PositionX = marker.GlobalPosition.X,
					PositionY = marker.GlobalPosition.Y
				});
			}
		}

		return availableSpawnPoints;
	}

	private async Task SpawnDeploymentAsync(Node currentScene, NpcDeploymentDto deployment)
	{
		if (!IsInsideTree() ||
			deployment.Status != "Deployed" ||
			!deployment.PositionX.HasValue ||
			!deployment.PositionY.HasValue ||
			NpcMapSprite.HasDeploymentSprite(currentScene, deployment.DeploymentId))
		{
			return;
		}

		Texture2D texture = null;
		if (string.IsNullOrWhiteSpace(deployment.AnimationScenePath))
		{
			try
			{
				texture = await AccountApiClient.LoadAvatarAsync(deployment.NpcImage);
			}
			catch (Exception exception)
			{
				GD.PrintErr(
					$"Image for deployed NPC {deployment.NpcName} (deployment {deployment.DeploymentId}) " +
					$"could not be loaded from '{deployment.NpcImage}': {exception.Message}");
				return;
			}

			if (texture == null)
			{
				GD.PrintErr(
					$"Image for deployed NPC {deployment.NpcName} (deployment {deployment.DeploymentId}) " +
					$"could not be decoded from '{deployment.NpcImage}'.");
				return;
			}
		}

		AnimatedSprite2D sprite;
		try
		{
			sprite = NpcMapSprite.CreateNpcSprite(texture, deployment.AnimationScenePath);
		}
		catch (Exception exception)
		{
			GD.PrintErr(
				$"Animation for deployed NPC {deployment.NpcName} (deployment {deployment.DeploymentId}) " +
				$"could not be loaded from '{deployment.AnimationScenePath}': {exception.Message}");
			return;
		}

		sprite.SetMeta("NpcId", deployment.NpcId);
		sprite.SetMeta("DeploymentId", deployment.DeploymentId);
		sprite.SetMeta("PositionMarker", deployment.PositionMarker ?? "");
		currentScene.AddChild(sprite);
		NpcMapSprite.PlaceAtGround(
			sprite,
			new Vector2(deployment.PositionX.Value, deployment.PositionY.Value));
	}
}
