using Godot;

public partial class AdminDashboard : DashboardBase
{
	public override void _Ready()
	{
		if (!RequireRole(UserRole.Admin))
		{
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

		popup.Clear();

		popup.AddItem("Profile", 0);
		popup.AddItem("Logout", 1);
		popup.AddItem("User Management", 2);
	}

	protected override void OnMenuItemPressed(long id)
	{
		if (id == 2)
		{
			NavigateTo(UserManagementPath);
			return;
		}

		base.OnMenuItemPressed(id);
	}
}
