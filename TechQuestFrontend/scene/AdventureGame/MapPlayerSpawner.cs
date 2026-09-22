using Godot;
using System;

public partial class MapPlayerSpawner : Node2D
{
    public override async void _Ready()
    {
        try
        {
            string scenePath = await SelectedCharacterService.GetOwnedSelectedScenePathAsync();
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                GD.PrintErr("No valid owned selected character was found.");
                RemoveExistingPlayer();
                return;
            }

            ReplacePlayer(scenePath);
        }
        catch (Exception error)
        {
            GD.PrintErr("Selected character load failed: " + error.Message);
        }
    }

    private void ReplacePlayer(string scenePath)
    {
        CharacterBody2D oldPlayer = FindExistingPlayer();
        Vector2 position = oldPlayer == null ? Vector2.Zero : oldPlayer.Position;
        Vector2 velocity = oldPlayer == null ? Vector2.Zero : oldPlayer.Velocity;

        PackedScene characterScene = GD.Load<PackedScene>(scenePath);
        if (characterScene == null)
            throw new InvalidOperationException("Character scene could not be loaded: " + scenePath);

        CharacterBody2D newPlayer = characterScene.Instantiate<CharacterBody2D>();
        newPlayer.Position = position;
        newPlayer.Velocity = velocity;
        AddChild(newPlayer);

        if (oldPlayer != null)
        {
            foreach (Node child in oldPlayer.GetChildren())
            {
                if (child is Camera2D camera)
                {
                    oldPlayer.RemoveChild(camera);
                    newPlayer.AddChild(camera);
                }
            }

            oldPlayer.QueueFree();
        }
    }

    private CharacterBody2D FindExistingPlayer()
    {
        foreach (Node child in GetChildren())
        {
            if (child is CharacterBody2D player)
                return player;
        }

        return null;
    }

    private void RemoveExistingPlayer()
    {
        CharacterBody2D oldPlayer = FindExistingPlayer();
        oldPlayer?.QueueFree();
    }
}