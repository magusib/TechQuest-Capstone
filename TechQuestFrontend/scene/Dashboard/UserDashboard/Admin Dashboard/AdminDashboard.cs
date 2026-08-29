using Godot;

public partial class AdminDashboard : DashboardBase
{
	public override void _Ready()
	{
		if (UserSession.Role == UserRole.None)
		{
			GD.PrintErr("No active user session. Returning to User Selection.");
			NavigateTo(UserSelectionPath);
			return;
		}

		BindNodes();
		ConnectCommonButtons();
		SetupCommonDisplay();

		ConfigureAdminPermissions();
		ConfigureAdminMenu();
	}

	private void ConfigureAdminPermissions()
	{
		SetButtonEnabled(HomeButton, true);
		SetButtonEnabled(RoomButton, true);
		SetButtonEnabled(LeaderboardButton, true);
		SetButtonEnabled(ShopButton, true);
		SetButtonEnabled(StorylineButton, true);

		// Character Dashboard is Student-only.
		// Admin character management is handled through Shop Management.
		SetButtonEnabled(CharacterButton, false);

		SetButtonEnabled(EasyButton, true);
		SetButtonEnabled(MediumButton, true);
		SetButtonEnabled(HardButton, true);
	}

	private void ConfigureAdminMenu()
	{
		if (MenuButton == null)
			return;

		PopupMenu popup = MenuButton.GetPopup();

		// Existing Admin scene needs 3 menu entries:
		// 0 = Profile, 1 = Logout, 2 = User Management.
		popup.Clear();

		popup.AddItem("Profile", 0);
		popup.AddItem("Logout", 1);
		popup.AddItem("User", 2);
	}

	protected override void OnMenuItemPressed(long id)
	{
		if (id == 2)
		{
			// Change this path when User Management scene is created.
			const string userManagementPath =
				"res://scene/UserManagement/UserManagement.tscn";

			NavigateTo(userManagementPath);
			return;
		}

		base.OnMenuItemPressed(id);
	}
}
