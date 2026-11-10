using Godot;
using System;

public abstract partial class DashboardBase : Control
{
	private ConfirmationDialog logoutConfirmationDialog;
	private bool responsiveLayoutConnected;

	// Existing Dashboard nodes
	protected TextureButton HomeButton;
	protected TextureButton RoomButton;
	protected TextureButton LeaderboardButton;
	protected TextureButton ShopButton;
	protected TextureButton CharacterButton;
	protected TextureButton TutorialButton;

	protected TextureButton EasyButton;
	protected TextureButton MediumButton;
	protected TextureButton HardButton;

	protected TextureRect Avatar;
	protected TextureButton AvatarButton;
	protected Label PlayerName;
	protected Panel CoinsPanel;
	protected Panel PointsPanel;
	protected Label CoinsLabel;
	protected Label PointsLabel;
	protected MenuButton MenuButton;

	protected virtual void BindNodes()
	{
		HomeButton = GetNodeOrNull<TextureButton>("HomePanel/HomeButton");
		RoomButton = GetNodeOrNull<TextureButton>("RoomPanel/RoomButton");
		LeaderboardButton = GetNodeOrNull<TextureButton>("LeaderboardPanel/LeaderboardButton");
		ShopButton = GetNodeOrNull<TextureButton>("ShopPanel/ShopButton");
		CharacterButton = GetNodeOrNull<TextureButton>("CharacterPanel/CharacterButton");
		TutorialButton = GetNodeOrNull<TextureButton>("TutorialPanel/TutorialButton");

		EasyButton = GetNodeOrNull<TextureButton>("DificultySelectionButton2/EasyButton");
		MediumButton = GetNodeOrNull<TextureButton>("DificultySelectionButton2/MediumButton");
		HardButton = GetNodeOrNull<TextureButton>("DificultySelectionButton2/HardButton");

		Avatar = GetNodeOrNull<TextureRect>("TopBar/AvatarPanel/Avatar");
		AvatarButton = GetNodeOrNull<TextureButton>("TopBar/AvatarPanel/AvatarButton");
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
		ConnectButton(TutorialButton, OnTutorialPressed);

		ConnectButton(AvatarButton, OnAvatarPressed);

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
		SetupResponsiveLayout();

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

	/// <summary>
	/// Keeps the desktop-authored dashboard usable on narrow/portrait screens.
	/// The scenes intentionally keep their original 1280x720 artwork positions;
	/// only a narrow viewport gets this alternate arrangement.
	/// </summary>
	private void SetupResponsiveLayout()
	{
		if (!responsiveLayoutConnected)
		{
			GetViewport().SizeChanged += ApplyResponsiveLayout;
			responsiveLayoutConnected = true;
		}

		ApplyResponsiveLayout();
	}

	private void ApplyResponsiveLayout()
	{
		if (!IsInsideTree())
			return;

		Vector2 viewport = GetViewportRect().Size;
		bool narrow = viewport.X < 900f || viewport.Y < 600f;
		if (!narrow)
			return;

		float width = Mathf.Max(viewport.X, 320f);
		float height = Mathf.Max(viewport.Y, 480f);
		float margin = Mathf.Clamp(width * 0.03f, 10f, 24f);
		float navSize = Mathf.Clamp((width - margin * 2f - 25f) / 6f, 48f, 68f);
		float navY = Mathf.Max(110f, height - navSize - margin);

		// Compact header: avatar/name on the left and menu on the right.
		Control topBar = GetNodeOrNull<Control>("TopBar");
		Control avatarPanel = GetNodeOrNull<Control>("TopBar/AvatarPanel");
		Place(topBar, new Vector2(margin, margin), new Vector2(width - margin * 2f, 82f));
		Place(avatarPanel, Vector2.Zero, new Vector2(Mathf.Min(width - 80f, 250f), 82f));
		Place(Avatar, new Vector2(4f, 4f), new Vector2(70f, 70f));
		Place(PlayerName, new Vector2(78f, 20f), new Vector2(Mathf.Min(160f, width - 170f), 42f));
		Place(MenuButton, new Vector2(width - margin * 2f - 58f, 8f), new Vector2(58f, 64f));

		// All dashboard actions share one touch-friendly bottom navigation row.
		Control[] panels =
		{
			GetNodeOrNull<Control>("HomePanel"),
			GetNodeOrNull<Control>("RoomPanel"),
			GetNodeOrNull<Control>("LeaderboardPanel"),
			GetNodeOrNull<Control>("ShopPanel"),
			GetNodeOrNull<Control>("CharacterPanel"),
			GetNodeOrNull<Control>("TutorialPanel")
		};
		BaseButton[] buttons = { HomeButton, RoomButton, LeaderboardButton, ShopButton, CharacterButton, TutorialButton };
		for (int i = 0; i < panels.Length; i++)
		{
			float x = margin + i * (navSize + 5f);
			Place(panels[i], new Vector2(x, navY), new Vector2(navSize, navSize));
			FitButton(panels[i], buttons[i]);
		}

		// Difficulty cards remain available, but are resized and stacked inside
		// the safe area so they cannot cover the header or bottom navigation.
		Control difficulty = GetNodeOrNull<Control>("DificultySelectionButton2");
		Place(difficulty, Vector2.Zero, viewport);
		TextureButton[] maps = { EasyButton, MediumButton, HardButton };
		float safeTop = 108f;
		float safeBottom = navY - 12f;
		float availableHeight = Mathf.Max(90f, safeBottom - safeTop);
		bool portrait = height > width * 1.15f;
		if (portrait)
		{
			float cardHeight = Mathf.Min(availableHeight / 3f - 8f, 118f);
			float cardWidth = Mathf.Min(width - margin * 2f, 360f);
			float cardX = (width - cardWidth) / 2f;
			for (int i = 0; i < maps.Length; i++)
				Place(maps[i], new Vector2(cardX, safeTop + i * (cardHeight + 8f)), new Vector2(cardWidth, cardHeight));
		}
		else
		{
			float cardWidth = Mathf.Min((width - margin * 2f - 16f) / 3f, 300f);
			float cardHeight = Mathf.Min(availableHeight - 8f, 150f);
			float startX = (width - (cardWidth * 3f + 16f * 2f)) / 2f;
			for (int i = 0; i < maps.Length; i++)
				Place(maps[i], new Vector2(startX + i * (cardWidth + 16f), safeTop + (availableHeight - cardHeight) / 2f), new Vector2(cardWidth, cardHeight));
		}
	}

	private static void Place(Control control, Vector2 position, Vector2 size)
	{
		if (control == null)
			return;

		control.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
		control.Position = position;
		control.Size = size;
	}

	private static void FitButton(Control panel, BaseButton button)
	{
		if (button is not Control control)
			return;

		control.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize, 4);
		control.MouseFilter = Control.MouseFilterEnum.Stop;
	}

	private async void SetAvatar()
	{
		if (!GodotObject.IsInstanceValid(Avatar) || string.IsNullOrWhiteSpace(UserSession.AvatarPath))
			return;

		TextureRect avatar = Avatar;
		try
		{
			Texture2D texture = await AccountApiClient.LoadAvatarAsync(UserSession.AvatarPath);
			if (IsInstanceValid(avatar) && texture != null)
				avatar.Texture = texture;
		}
		catch (Exception error)
		{
			GD.PrintErr($"Dashboard avatar load failed: {error.Message}");
		}
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

	protected const string UserManagementPath =
		"res://scene/UserManagement/UserManagement.tscn";

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
		"res://scene/Dashboard/Modules/Tutorial/StudentTutorial.tscn";

	protected const string ProfessorStorylinePath =
		"res://scene/Dashboard/Modules/Tutorial/ProfessorTutorial.tscn";

	protected const string AdminStorylinePath =
		"res://scene/Dashboard/Modules/Tutorial/AdminTutorial.tscn";

	protected const string EasyMapPath =
		"res://scene/AdventureGame/Difficulty/EasyLevelMap/EasyMap.tscn";

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

	protected virtual void OnAvatarPressed()
	{
		OpenProfile();
	}

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

	protected virtual void OnTutorialPressed()
	{
		// Each dashboard embeds its tutorial as a popup. Keep the tutorial on
		// the dashboard for every role instead of routing Admin/Professor to a
		// separate scene.
		if (UserSession.Role == UserRole.Student ||
			UserSession.Role == UserRole.Professor ||
			UserSession.Role == UserRole.Admin)
		{
			string popupName = UserSession.Role switch
			{
				UserRole.Student => "StudentTutorialPopup",
				UserRole.Professor => "ProfessorTutorialPopup",
				UserRole.Admin => "AdminTutorialPopup",
				_ => ""
			};
			Control tutorialPopup = GetNodeOrNull<Control>(popupName);
			if (tutorialPopup != null)
			{
				tutorialPopup.Visible = true;
				return;
			}
		}

		string tutorialPath = UserSession.Role switch
		{
			UserRole.Student => StudentStorylinePath,
			UserRole.Professor => ProfessorStorylinePath,
			UserRole.Admin => AdminStorylinePath,
			_ => ""
		};
		NavigateTo(tutorialPath);
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
					NavigateTo(UserManagementPath);
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
		string popupName = UserSession.Role switch
		{
			UserRole.Student => "StudentProfilePopup",
			UserRole.Professor => "ProfessorProfilePopup",
			UserRole.Admin => "AdminProfilePopup",
			_ => ""
		};

		if (!string.IsNullOrWhiteSpace(popupName))
		{
			Control profilePopup = GetNodeOrNull<Control>(popupName);
			if (profilePopup != null)
			{
				profilePopup.Visible = true;
				profilePopup.MoveToFront();
				return;
			}
		}

		// Keep standalone-scene navigation as a fallback for dashboards that do
		// not yet embed their profile popup.
		string profilePath = UserSession.Role switch
		{
			UserRole.Student => StudentProfilePath,
			UserRole.Professor => ProfessorProfilePath,
			UserRole.Admin => AdminProfilePath,
			_ => ""
		};
		NavigateTo(profilePath);
	}

	protected virtual void Logout()
	{
		ConfirmLogout();
	}
}
