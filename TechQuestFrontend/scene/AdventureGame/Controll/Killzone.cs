using Godot;
using System;

public partial class Killzone : Area2D
{
    private bool deathProcessed;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        GetNode<Timer>("Timer").Timeout += OnTimerTimeout;
    }

    private async void OnBodyEntered(Node2D body)
    {
        if (deathProcessed || body is not Character)
            return;

        deathProcessed = true;
        try
        {
            LifespanResponseDto response = await LifespanApiClient.DeductAsync(5, "killzone");
            UserSession.ApplyLifespanResponse(response);
        }
        catch (Exception exception)
        {
            GD.PrintErr("Killzone lifespan deduction failed: " + exception.Message);
        }

        GetNode<Timer>("Timer").Start();
    }

    private void OnTimerTimeout()
    {
        GetTree().ReloadCurrentScene();
    }
}
