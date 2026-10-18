using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class NpcDeploymentRenderer : Node
{
	[Export]
	public string Difficulty { get; set; } = "Easy";

	public override void _Ready()
	{
		LoadDeploymentsAsync();
	}

	private async void LoadDeploymentsAsync()
	{
		try
		{
			List<NpcDeploymentDto> deployments = await NpcApiClient.GetDeploymentsAsync(Difficulty);
			foreach (NpcDeploymentDto deployment in deployments)
			{
				if (!IsInsideTree())
					return;

				List<NpcDeploymentDto> activeDeployments = await NpcApiClient.GetDeploymentsAsync(Difficulty);
				if (!activeDeployments.Exists(current => current.DeploymentId == deployment.DeploymentId))
					continue;
				if (NpcMapSprite.HasDeploymentSprite(GetTree().CurrentScene, deployment.DeploymentId))
					continue;

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
						continue;
					}

					if (texture == null)
					{
						GD.PrintErr(
							$"Image for deployed NPC {deployment.NpcName} (deployment {deployment.DeploymentId}) " +
							$"could not be decoded from '{deployment.NpcImage}'.");
						continue;
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
					continue;
				}

				sprite.SetMeta("NpcId", deployment.NpcId);
				sprite.SetMeta("DeploymentId", deployment.DeploymentId);
				GetTree().CurrentScene.AddChild(sprite);
				NpcMapSprite.PlaceAtGround(
					sprite,
					new Vector2(deployment.PositionX, deployment.PositionY));
			}
		}
		catch (Exception exception)
		{
			GD.PrintErr($"NPC deployments for the {Difficulty} map could not be loaded: {exception.Message}");
		}
	}
}
