#nullable enable

using Godot;
using System;
using System.Collections.Generic;

public partial class DashboardController : Control
{
	[Export] public string RequiredRole { get; set; } = "";

	private readonly Dictionary<string, string> destinations = new(StringComparer.OrdinalIgnoreCase)
	{
		["easy"] = "res://scene/Dashboard/Easy/EasyMap.tscn",
		["medium"] = "res://scene/Dashboard/Medium/MediumMap.tscn",
		["hard"] = "res://scene/Dashboard/Hard/HardMap.tscn",
		["room"] = "res://scene/Dashboard/Room/RoomDashboard.tscn",
		["leaderboard"] = "res://scene/Dashboard/Leaderboard/LeaderboardDashboard.tscn",
		["shop"] = "res://scene/Dashboard/Shop/ShopDashboard.tscn",
		["character"] = "res://scene/Dashboard/Character/CharacterDashboard.tscn",
		["storyline"] = "res://scene/Dashboard/Storyline/StorylineDashboard.tscn",
		["student-profile"] = "res://scene/Dashboard/UserDashboard/Student Dashboard/StudentProfile.tscn",
		["professor-profile"] = "res://scene/Dashboard/UserDashboard/Professor Dashboard/ProfessorProfile.tscn",
		["admin-profile"] = "res://scene/Dashboard/UserDashboard/Admin Dashboard/AdminProfile.tscn",
		["user-management"] = "res://scene/Dashboard/UserManagement/UserManagement.tscn"
	};

	private readonly Dictionary<string, BaseButton> buttons = new(StringComparer.OrdinalIgnoreCase);
	private PopupMenu? menu;
	private Label? playerName;
	private Label? coinsLabel;
	private Label? pointsLabel;
	private TextureRect? avatar;
	private AcceptDialog? message;
	private ConfirmationDialog? logoutConfirmation;
	private string role = "";

	public override void _Ready()
	{
		role = AccountSession.Current?.Role ?? AccountSession.NormalizeRole(RequiredRole);
		if (string.IsNullOrWhiteSpace(role))
		{
			ReturnToLogin();
			return;
		}

		BindNodes();
		ConfigurePermissions();
		PopulateAccount();
	}

	private void BindNodes()
	{
		BindButton("HomeButton", "home");
		logoutConfirmation = new ConfirmationDialog
		{
			Title = "Logout",
			DialogText = "Are you sure you want to logout?"
		};
		AddChild(logoutConfirmation);
		logoutConfirmation.GetOkButton().Text = "Yes";
		logoutConfirmation.GetCancelButton().Text = "No";
		logoutConfirmation.Confirmed += ConfirmLogout;
		BindButton("RoomButton", "room");
		BindButton("LeaderboardButton", "leaderboard");
		BindButton("ShopButton", "shop");
		BindButton("CharacterButton", "character");
		BindButton("StorylineButton", "storyline");
		BindButton("DificultySelectionButton2/EasyButton", "easy");
		BindButton("DificultySelectionButton2/MediumButton", "medium");
		BindButton("DificultySelectionButton2/HardButton", "hard");

		menu = GetNodeOrNull<PopupMenu>("TopBar/MenuButton/PopupMenu");
		{
			var menuButton = GetNodeOrNull<MenuButton>("TopBar/MenuButton");
			menu = menuButton?.GetPopup();
		}

		if (menu != null)
		{
			menu.IdPressed += OnMenuItemPressed;
		}

		playerName = GetNodeOrNull<Label>("TopBar/AvatarPanel/PlayerName");
		coinsLabel = GetNodeOrNull<Label>("TopBar/AvatarPanel/CoinsPanel/CointsLabel");
		pointsLabel = GetNodeOrNull<Label>("TopBar/AvatarPanel/PointsPanel/PointsLabel");
		avatar = GetNodeOrNull<TextureRect>("TopBar/AvatarPanel/Avatar");
		message = GetNodeOrNull<AcceptDialog>("MessageDialog");

		logoutConfirmation = new ConfirmationDialog
		{
			Title = "Logout",
			DialogText = "Are you sure you want to logout?"
		};
		AddChild(logoutConfirmation);
		logoutConfirmation.GetOkButton().Text = "Yes";
		logoutConfirmation.GetCancelButton().Text = "No";
		logoutConfirmation.Confirmed += ConfirmLogout;
	}

	private void BindButton(string path, string action)
	{
		var button = GetNodeOrNull<BaseButton>(path);
		if (button == null)
		{
			return;
		}

		buttons[action] = button;
		button.Pressed += () => Navigate(action);
	}

	private void ConfigurePermissions()
	{
		bool isProfessor = role == "professor";
		bool isStudent = role == "student";

		SetEnabled("easy", !isProfessor);
		SetEnabled("medium", !isProfessor);
		SetEnabled("hard", !isProfessor);
		SetEnabled("character", isStudent);

		if (coinsLabel?.GetParent() is CanvasItem coinsPanel)
		{
			coinsPanel.Visible = isStudent;
		}

		if (pointsLabel?.GetParent() is CanvasItem pointsPanel)
		{
			pointsPanel.Visible = isStudent;
		}

		if (menu != null)
		{
			bool hasUserManagement = false;
			for (int index = 0; index < menu.GetItemCount(); index++)
			{
				hasUserManagement = hasUserManagement || menu.GetItemId(index) == 2;
			}

			if (menu.GetItemCount() == 0)
			{
				menu.AddItem("Profile", 0);
				if (role == "admin")
				{
					menu.AddItem("User Management", 2);
				}
				menu.AddItem("Logout", 1);
			}
			else if (role == "admin" && !hasUserManagement)
			{
				menu.AddItem("User Management", 2);
			}
		}
	}

	private void SetEnabled(string action, bool enabled)
	{
		if (buttons.TryGetValue(action, out var button))
		{
			button.Disabled = !enabled;
		}
	}

	private void PopulateAccount()
	{
		var session = AccountSession.Current;
		if (session == null)
		{
			return;
		}

		if (playerName != null)
		{
			string name = (session.FirstName + " " + session.LastName).Trim();
			playerName.Text = string.IsNullOrWhiteSpace(name) ? RoleTitle() : name;
		}

		if (coinsLabel != null)
		{
			coinsLabel.Text = $"Coins: {session.Coins}";
		}

		if (pointsLabel != null)
		{
			pointsLabel.Text = $"Points: {session.Points}";
		}

		if (!string.IsNullOrWhiteSpace(session.AvatarPath) && ResourceLoader.Exists(session.AvatarPath))
		{
			avatar?.Set("texture", ResourceLoader.Load<Texture2D>(session.AvatarPath));
		}
	}

	private void Navigate(string action)
	{
		if (action == "home")
		{
			return;
		}

		if (!CanAccess(action))
		{
			ShowMessage("You do not have permission to access this feature.");
			return;
		}

		if (!destinations.TryGetValue(action, out var path))
		{
			ShowMessage("This feature is not configured yet.");
			return;
		}

		if (!ResourceLoader.Exists(path))
		{
			ShowMessage("This dashboard screen is not available yet.");
			return;
		}

		GetTree().ChangeSceneToFile(path);
	}

	private bool CanAccess(string action)
	{
		if (role == "professor" && (action == "easy" || action == "medium" || action == "hard" || action == "character"))
		{
			return false;
		}

		if (role == "student" && action == "user-management")
		{
			return false;
		}

		return role == "student" || role == "professor" || role == "admin";
	}

	private void OnMenuItemPressed(long id)
	{
		switch (id)
		{
			case 0:
				Navigate(role + "-profile");
				break;
			case 1:
				RequestLogout();
				break;
			case 2:
				if (role == "admin")
				{
					Navigate("user-management");
				}
				else
				{
					ShowMessage("You do not have permission to access user management.");
				}
				break;
		}
	}

	private void RequestLogout()
	{
		logoutConfirmation?.PopupCentered();
	}

	private void ConfirmLogout()
	{
		AccountSession.Current?.Clear();
		GetTree().ChangeSceneToFile("res://scene/LandingPage/LandingPage.tscn");
	}

	private void ReturnToLogin()
	{
		GetTree().ChangeSceneToFile("res://scene/UserSelection/UserSelection.tscn");
	}

	private void ShowMessage(string text)
	{
		if (message == null)
		{
			GD.Print(text);
			return;
		}

		message.DialogText = text;
		message.PopupCentered();
	}

	private string RoleTitle()
	{
		return role switch
		{
			"student" => "Student Name",
			"professor" => "Professor Name",
			"admin" => "Admin Name",
			_ => "User"
		};
	}
}
