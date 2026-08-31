using Godot;
using System;

public abstract partial class DashboardBase : Control
{
	private ConfirmationDialog logoutConfirmationDialog;

	// Existing Dashboard nodes
	protected TextureButton HomeButton;
	protected TextureButton RoomButton;
	protected TextureButton LeaderboardButton;
	protected TextureButton ShopButton;
	protected TextureButton CharacterButton;
	protected TextureButton StorylineButton;

	protected TextureButton EasyButton;
	protected TextureButton MediumButton;
	protected TextureButton HardButton;

	protected TextureRect Avatar;
	protected Label PlayerName;
	protected Panel CoinsPanel;
	protected Panel PointsPanel;
	protected Label CoinsLabel;
	protected Label PointsLabel;
	protected MenuButton MenuButton;

	protected virtual void BindNodes()
	{
		HomeButton = GetNodeOrNull<TextureButton>("HomeButton");
		RoomButton = GetNodeOrNull<TextureButton>("RoomButton");
		LeaderboardButton = GetNodeOrNull<TextureButton>("LeaderboardButton");
		ShopButton = GetNodeOrNull<TextureButton>("ShopButton");
		CharacterButton = GetNodeOrNull<TextureButton>("CharacterButton");
		StorylineButton = GetNodeOrNull<TextureButton>("StorylineButton");

		EasyButton = GetNodeOrNull<TextureButton>("DificultySelectionButton2/EasyButton");
		MediumButton = GetNodeOrNull<TextureButton>("DificultySelectionButton2/MediumButton");
		HardButton = GetNodeOrNull<TextureButton>("DificultySelectionButton2/HardButton");

		Avatar = GetNodeOrNull<TextureRect>("TopBar/AvatarPanel/Avatar");
		PlayerName = GetNodeOrNull<Label>("TopBar/AvatarPanel/PlayerName");
		CoinsPanel = GetNodeOrNull<Panel>("TopBar/AvatarPanel/CoinsPanel");
		PointsPanel = GetNodeOrNull<Panel>("TopBar/AvatarPanel/PointsPanel");

		// Existing scene uses the spelling "CointsLabel".
		CoinsLabel = GetNodeOrNull<Label>("TopBar/AvatarPanel/CoinsPanel/CointsLabel");
		PointsLabel = GetNodeOrNull<Label>("TopBar/AvatarPanel/PointsPanel/PointsLabel");

		MenuButton = GetNodeOrNull<MenuButton>("TopBar/MenuButton");
	}

	protected virtual void ConnectCommonButtons()
	{
		ConnectButton(HomeButton, OnHomePressed);
		ConnectButton(RoomButton, OnRoomPressed);
		ConnectButton(LeaderboardButton, OnLeaderboardPressed);
		ConnectButton(ShopButton, OnShopPressed);
		ConnectButton(CharacterButton, OnCharacterPressed);
		ConnectButton(StorylineButton, OnStorylinePressed);

		ConnectButton(EasyButton, OnEasyPressed);
		ConnectButton(MediumButton, OnMediumPressed);
		ConnectButton(HardButton, OnHardPressed);

		if (MenuButton != null)
			MenuButton.GetPopup().IdPressed += OnMenuItemPressed;
	}

	private void ConnectButton(BaseButton button, Action callback)
	{
		if (button == null)
			return;

		button.Pressed += callback;
	}

	protected void SetupCommonDisplay()
	{
		if (PlayerName != null)
			PlayerName.Text = UserSession.FullName;

		bool isStudent = UserSession.HasRole(UserRole.Student);

		if (CoinsPanel != null)
			CoinsPanel.Visible = isStudent;

		if (PointsPanel != null)
			PointsPanel.Visible = isStudent;

		if (CoinsLabel != null)
			CoinsLabel.Text = UserSession.Coins.ToString();

		if (PointsLabel != null)
			PointsLabel.Text = UserSession.Points.ToString();

		SetAvatar();
	}

	private void SetAvatar()
	{
		if (Avatar == null || string.IsNullOrWhiteSpace(UserSession.AvatarPath))
			return;

		if (!ResourceLoader.Exists(UserSession.AvatarPath))
		{
			GD.PrintErr($"Avatar resource not found: {UserSession.AvatarPath}");
			return;
		}

		Texture2D texture = GD.Load<Texture2D>(UserSession.AvatarPath);
		if (texture != null)
			Avatar.Texture = texture;
	}

	protected void SetButtonEnabled(BaseButton button, bool enabled)
	{
		if (button == null)
			return;

		button.Disabled = !enabled;
		button.MouseDefaultCursorShape =
			enabled
				? Control.CursorShape.PointingHand
				: Control.CursorShape.Forbidden;
	}

	protected void SetButtonVisible(Control button, bool visible)
	{
		if (button != null)
			button.Visible = visible;
	}

	// ---------------------------------------------------------
	// Navigation
	// Change these paths when your actual module .tscn files
	// are created. The current uploaded project does not yet
	// contain Room/Shop/Leaderboard/Storyline .tscn files.
	// ---------------------------------------------------------

	protected const string StudentDashboardPath =
		"res://scene/Dashboard/UserDashboard/Student Dashboard/StudentDashboard.tscn";

	protected const string ProfessorDashboardPath =
		"res://scene/Dashboard/UserDashboard/Professor Dashboard/ProfessorDashboard.tscn";

	protected const string AdminDashboardPath =
		"res://scene/Dashboard/UserDashboard/Admin Dashboard/AdminDashboard.tscn";

	protected const string StudentProfilePath =
		"res://scene/Dashboard/UserDashboard/Student Dashboard/StudentProfile.tscn";

	protected const string ProfessorProfilePath =
		"res://scene/Dashboard/UserDashboard/Professor Dashboard/ProfessorProfile.tscn";

	protected const string AdminProfilePath =
		"res://scene/Dashboard/UserDashboard/Admin Dashboard/AdminProfile.tscn";

	protected const string UserSelectionPath =
		"res://scene/UserSelection/UserSelection.tscn";

	protected const string LandingPagePath =
		"res://scene/LandingPage/LandingPage.tscn";

	protected const string StudentRoomPath =
		"res://scene/Dashboard/Modules/Room/StudentRoom.tscn";

	protected const string ProfessorRoomPath =
		"res://scene/Dashboard/Modules/Room/ProfessorRoom.tscn";

	protected const string AdminRoomPath =
		"res://scene/Dashboard/Modules/Room/AdminRoom.tscn";

	protected const string StudentLeaderboardPath =
		"res://scene/Dashboard/Modules/Leaderboard/StudentLeaderboard.tscn";

	protected const string ProfessorLeaderboardPath =
		"res://scene/Dashboard/Modules/Leaderboard/ProfessorLeaderboard.tscn";

	protected const string AdminLeaderboardPath =
		"res://scene/Dashboard/Modules/Leaderboard/AdminLeaderboard.tscn";

	protected const string StudentShopPath =
		"res://scene/Dashboard/Modules/Shop/StudentShop.tscn";

	protected const string ProfessorShopPath =
		"res://scene/Dashboard/Modules/Shop/ProfessorShop.tscn";

	protected const string AdminShopPath =
		"res://scene/Dashboard/Modules/Shop/AdminShop.tscn";

	protected const string StudentCharacterPath =
		"res://scene/Dashboard/Modules/Character/StudentCharacter.tscn";

	protected const string StudentStorylinePath =
		"res://scene/Dashboard/Modules/Storyline/StudentStoryline.tscn";

	protected const string ProfessorStorylinePath =
		"res://scene/Dashboard/Modules/Storyline/ProfessorStoryline.tscn";

	protected const string AdminStorylinePath =
		"res://scene/Dashboard/Modules/Storyline/AdminStoryline.tscn";

	protected const string EasyMapPath =
		"res://scene//AdventureGame/Difficulty/EasyLevelMap/EasyMap.tscn";

	protected const string MediumMapPath =
		"res://scene/AdventureGame/Difficulty/MediumLevelMap/MediumMap.tscn";

	protected const string HardMapPath =
		"res://scene/AdventureGame/Difficulty/HardLevelMap/HardMap.tscn";

	protected void NavigateTo(string scenePath)
	{
		if (string.IsNullOrWhiteSpace(scenePath))
			return;

		if (!ResourceLoader.Exists(scenePath))
		{
			GD.PrintErr($"Dashboard navigation failed. Scene does not exist: {scenePath}");
			return;
		}

		GetTree().ChangeSceneToFile(scenePath);
	}

	protected bool RequireRole(UserRole requiredRole)
	{
		if (UserSession.HasRole(requiredRole))
			return true;

		GD.PrintErr(
			$"Access denied. Required role: {requiredRole}; current role: {UserSession.Role}"
		);

		return false;
	}

	// ---------------------------------------------------------
	// Common button handlers
	// ---------------------------------------------------------

	protected virtual void OnHomePressed()
	{
		switch (UserSession.Role)
		{
			case UserRole.Student:
				NavigateTo(StudentDashboardPath);
				break;

			case UserRole.Professor:
				NavigateTo(ProfessorDashboardPath);
				break;

			case UserRole.Admin:
				NavigateTo(AdminDashboardPath);
				break;

			default:
				NavigateTo(UserSelectionPath);
				break;
		}
	}

	protected virtual void OnRoomPressed()
	{
		switch (UserSession.Role)
		{
			case UserRole.Student:
				NavigateTo(StudentRoomPath);
				break;

			case UserRole.Professor:
				NavigateTo(ProfessorRoomPath);
				break;

			case UserRole.Admin:
				NavigateTo(AdminRoomPath);
				break;
		}
	}

	protected virtual void OnLeaderboardPressed()
	{
		switch (UserSession.Role)
		{
			case UserRole.Student:
				NavigateTo(StudentLeaderboardPath);
				break;

			case UserRole.Professor:
				NavigateTo(ProfessorLeaderboardPath);
				break;

			case UserRole.Admin:
				NavigateTo(AdminLeaderboardPath);
				break;
		}
	}

	protected virtual void OnShopPressed()
	{
		switch (UserSession.Role)
		{
			case UserRole.Student:
				NavigateTo(StudentShopPath);
				break;

			case UserRole.Professor:
				NavigateTo(ProfessorShopPath);
				break;

			case UserRole.Admin:
				NavigateTo(AdminShopPath);
				break;
		}
	}

	protected virtual void OnCharacterPressed()
	{
		if (!RequireRole(UserRole.Student))
			return;

		NavigateTo(StudentCharacterPath);
	}

	protected virtual void OnStorylinePressed()
	{
		switch (UserSession.Role)
		{
			case UserRole.Student:
				NavigateTo(StudentStorylinePath);
				break;

			case UserRole.Professor:
				NavigateTo(ProfessorStorylinePath);
				break;

			case UserRole.Admin:
				NavigateTo(AdminStorylinePath);
				break;
		}
	}

	protected virtual void OnEasyPressed()
	{
		if (UserSession.Role != UserRole.Student &&
			UserSession.Role != UserRole.Admin)
			return;

		NavigateTo(EasyMapPath);
	}

	protected virtual void OnMediumPressed()
	{
		if (UserSession.Role != UserRole.Student &&
			UserSession.Role != UserRole.Admin)
			return;

		NavigateTo(MediumMapPath);
	}

	protected virtual void OnHardPressed()
	{
		if (UserSession.Role != UserRole.Student &&
			UserSession.Role != UserRole.Admin)
			return;

		NavigateTo(HardMapPath);
	}

	protected virtual void OnMenuItemPressed(long id)
	{
		switch (id)
		{
			case 0:
				OpenProfile();
				break;

			case 1:
				ShowLogoutConfirmation();
				break;

			case 2:
				if (UserSession.HasRole(UserRole.Admin))
					NavigateTo(UserSelectionPath);
				break;
		}
	}

	private void ShowLogoutConfirmation()
	{
		if (logoutConfirmationDialog == null)
		{
			logoutConfirmationDialog = new ConfirmationDialog();
			logoutConfirmationDialog.Title = "Logout";
			logoutConfirmationDialog.DialogText = "Are you sure you want to logout?";
			logoutConfirmationDialog.OkButtonText = "Yes";
			logoutConfirmationDialog.CancelButtonText = "No";
			logoutConfirmationDialog.MinSize = new Vector2I(360, 150);
			logoutConfirmationDialog.Confirmed += ConfirmLogout;
			logoutConfirmationDialog.Canceled += OnLogoutCancellation;
			AddChild(logoutConfirmationDialog);
		}

		logoutConfirmationDialog.PopupCentered();
		logoutConfirmationDialog.GrabFocus();
	}

	private void ConfirmLogout()
	{
		UserSession.Clear();
		NavigateTo(LandingPagePath);
	}

	private void OnLogoutCancellation()
	{
		if (logoutConfirmationDialog != null)
			logoutConfirmationDialog.Hide();
	}

	protected virtual void OpenProfile()
	{
		switch (UserSession.Role)
		{
			case UserRole.Student:
				NavigateTo(StudentProfilePath);
				break;

			case UserRole.Professor:
				NavigateTo(ProfessorProfilePath);
				break;

			case UserRole.Admin:
				NavigateTo(AdminProfilePath);
				break;
		}
	}

	protected virtual void Logout()
	{
		ConfirmLogout();
	}
}
