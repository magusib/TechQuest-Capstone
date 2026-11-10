using Godot;
using System;

public partial class Killzone : Area2D
{
	private bool deathProcessed;
	private Control gameOverPopup;
	private Label lifespanLabel;
	private Button okButton;
	private AudioStreamPlayer deathAudio;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		gameOverPopup = GetNode<Control>("GameOverLayer/GameOverPopup");
		lifespanLabel = GetNode<Label>("GameOverLayer/GameOverPopup/DimBackground/CenterContainer/Panel/MarginContainer/Content/LifespanLabel");
		okButton = GetNode<Button>("GameOverLayer/GameOverPopup/DimBackground/CenterContainer/Panel/MarginContainer/Content/OkButton");
		deathAudio = GetNode<AudioStreamPlayer>("DeathAudio");

		okButton.Pressed += ReturnToMap;
		gameOverPopup.Hide();
	}

	private async void OnBodyEntered(Node2D body)
	{
		if (deathProcessed || body is not Character)
			return;

		deathProcessed = true;

		// Hide and stop the player immediately when the kill zone is touched.
		// This must happen before awaiting the lifespan API call; otherwise the
		// character remains visible behind the Game Over popup.
		Character character = (Character)body;
		character.Hide();
		character.Velocity = Vector2.Zero;
		character.ProcessMode = Node.ProcessModeEnum.Disabled;

		deathAudio.Play();
		try
		{
			LifespanResponseDto response = await LifespanApiClient.DeductAsync(5, "killzone");
			UserSession.ApplyLifespanResponse(response);
		}
		catch (Exception exception)
		{
			GD.PrintErr("Killzone lifespan deduction failed: " + exception.Message);
		}

		lifespanLabel.Text = $"Lifespan -5\nRemaining Lifespan: {UserSession.Lifespan} / {UserSession.MaximumLifespan}";
		gameOverPopup.Show();
		okButton.GrabFocus();
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (!gameOverPopup.Visible || @event.IsEcho())
			return;

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Enter)
			ReturnToMap();
	}

	private void ReturnToMap()
	{
		if (!gameOverPopup.Visible)
			return;

		GetTree().ReloadCurrentScene();
	}
}
