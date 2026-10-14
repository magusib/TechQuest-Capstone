using Godot;
using System;

public partial class NpcDeploymentRenderer : Node
{
	[Export]
	public string Difficulty { get; set; } = "Easy";

	public override void _Ready()
	{
		try
		{
			foreach (NpcDeploymentDto deployment in NpcApiClient.GetDeployments(Difficulty))
			{
				Texture2D texture = AccountApiClient.LoadAvatarAsync(deployment.NpcImage).GetAwaiter().GetResult();
				if (texture == null)
					throw new InvalidOperationException($"Image for NPC {deployment.NpcName} could not be loaded.");

				AnimatedSprite2D sprite = NpcMapSprite.CreateIdleSprite(texture);
				sprite.SetMeta("NpcId", deployment.NpcId);
				GetTree().CurrentScene.AddChild(sprite);
				sprite.GlobalPosition = new Vector2(deployment.PositionX, deployment.PositionY);
			}
		}
		catch (Exception exception)
		{
			GD.PrintErr($"NPC deployments for the {Difficulty} map could not be loaded: {exception.Message}");
		}
	}
}
