
using Godot;
using System.Collections.Generic;

public partial class ButtonSound : Node
{
	[Export]
	public NodePath RootPath { get; set; } = new NodePath();

	private readonly Dictionary<string, AudioStreamPlayer> sounds = new();

	public override void _Ready()
	{
		if (RootPath.IsEmpty)
		{
			GD.PushError("Empty RootPath for Interface Sounds!");
			return;
		}

		// Create audio players
		CreateSound("UI_Hover");
		CreateSound("UI_Click");

		// Get the target root node
		Node root = GetNodeOrNull<Node>(RootPath);

		if (root == null)
		{
			GD.PushError($"Root node not found: {RootPath}");
			return;
		}

		// Install sounds on all buttons
		InstallSounds(root);
	}

	// Create and load audio players
	private void CreateSound(string soundName)
	{
		var player = new AudioStreamPlayer();

		player.Name = soundName;

		// Load WAV files from Audio folder
		player.Stream = GD.Load<AudioStream>(
			$"res://Audio/{soundName}.wav"
		);

		// Assign audio bus
		player.Bus = "Sfx";

		// Add player to scene tree
		AddChild(player);

		sounds[soundName] = player;
	}

	// Detect buttons recursively
	private void InstallSounds(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			// Button, TextureButton, OptionButton
			if (child is BaseButton button)
			{
				button.MouseEntered += () =>
					PlaySfx("UI_Hover");

				button.Pressed += () =>
					PlaySfx("UI_Click");
			}

			// TabContainer sounds
			if (child is TabContainer tabs)
			{
				tabs.TabHovered += (long _) =>
					PlaySfx("UI_Hover");

				tabs.TabClicked += (long _) =>
					PlaySfx("UI_Click");
			}

			// Recursively detect nested buttons
			InstallSounds(child);
		}
	}

	// Play UI sound
	public void PlaySfx(string soundName)
	{
		if (sounds.TryGetValue(soundName, out AudioStreamPlayer player))
		{
			if (player.Stream != null)
			{
				player.Play();
			}
		}
	}
}
