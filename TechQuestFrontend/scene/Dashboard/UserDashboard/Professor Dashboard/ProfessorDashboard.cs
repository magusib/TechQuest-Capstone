using Godot;

public partial class ProfessorDashboard : DashboardBase
{
	public override void _Ready()
	{
		if (!RequireRole(UserRole.Professor))
		{
			NavigateTo(UserSelectionPath);
			return;
		}

		BindNodes();
		ConnectCommonButtons();
		SetupCommonDisplay();

		ConfigureProfessorPermissions();
	}

	private void ConfigureProfessorPermissions()
	{
		// Professor can access:
		// Home, Room, Leaderboard, Shop, Storyline, Profile, Logout.
		// Professor cannot access difficulty maps or Student Character Dashboard.

		SetButtonEnabled(HomeButton, true);
		SetButtonEnabled(RoomButton, true);
		SetButtonEnabled(LeaderboardButton, true);
		SetButtonEnabled(ShopButton, true);
		SetButtonEnabled(StorylineButton, true);

		SetButtonEnabled(EasyButton, false);
		SetButtonEnabled(MediumButton, false);
		SetButtonEnabled(HardButton, false);
		SetButtonEnabled(CharacterButton, false);
	}
}
