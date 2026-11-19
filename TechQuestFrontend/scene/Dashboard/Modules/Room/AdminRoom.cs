using Godot;
using System;

public partial class AdminRoom : Control
{
	private Button backButton;
	private AcceptDialog accessPopup;

	public override void _Ready()
	{
		if (!FeatureAccessGuard.Require(this, FeatureAccessGuard.Room, FeatureAccessGuard.DashboardMessage))
			return;

		backButton = GetNode<Button>("BackButton");
		accessPopup = GetNode<AcceptDialog>("AdminRoomAccessPopup");
		
		backButton.Pressed += OnBackPressed;
		CallDeferred(nameof(ShowAccessRestricted));

	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/UserDashboard/Admin Dashboard/AdminDashboard.tscn");
	}

	private void ShowAccessRestricted()
	{
		accessPopup.PopupCentered();
	}
}
