using Godot;

public partial class StudentDashboard : DashboardBase
{
	public override void _Ready()
	{
		if (!RequireRole(UserRole.Student))
		{
			NavigateTo(UserSelectionPath);
			return;
		}

		BindNodes();
		ConnectCommonButtons();
		SetupCommonDisplay();

		ConfigureStudentPermissions();
	}

	private void ConfigureStudentPermissions()
	{
		SetButtonEnabled(HomeButton, true);
		SetButtonEnabled(RoomButton, true);
		SetButtonEnabled(LeaderboardButton, true);
		SetButtonEnabled(ShopButton, true);
		SetButtonEnabled(CharacterButton, true);
		SetButtonEnabled(StorylineButton, true);

		SetButtonEnabled(EasyButton, true);
		SetButtonEnabled(MediumButton, true);
		SetButtonEnabled(HardButton, true);
	}
}
