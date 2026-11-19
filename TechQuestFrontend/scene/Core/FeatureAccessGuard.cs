using Godot;

/// <summary>
/// Centralizes the existing TechQuest role rules for dashboard features and
/// protects feature scenes that are opened without using dashboard buttons.
/// </summary>
public static class FeatureAccessGuard
{
	public const string DashboardMessage =
		"You are not authorized to access this feature. Please use the features available for your account.";
	public const string DifficultyMessage =
		"You are not authorized to access the Difficulty selection feature.";

	public const string Home = "home";
	public const string Room = "room";
	public const string Leaderboard = "leaderboard";
	public const string Shop = "shop";
	public const string Character = "character";
	public const string Tutorial = "tutorial";
	public const string Difficulty = "difficulty";

	public static bool IsAuthorized(string feature)
	{
		return feature switch
		{
			Home or Leaderboard or Shop or Tutorial =>
				UserSession.IsStudent || UserSession.IsProfessor || UserSession.IsAdmin,
			Room => UserSession.IsStudent || UserSession.IsProfessor,
			Character => UserSession.IsStudent,
			Difficulty => UserSession.IsStudent || UserSession.IsAdmin,
			_ => false
		};
	}

	public static bool Require(Node owner, string feature, string message)
	{
		if (IsAuthorized(feature))
			return true;

		ShowAccessRestricted(owner, message, true);
		return false;
	}

	public static void ShowAccessRestricted(Node owner, string message, bool returnToDashboard = false)
	{
		AcceptDialog dialog = new AcceptDialog
		{
			Title = "Access Restricted",
			DialogText = message,
			OkButtonText = "OK"
		};

		float viewportWidth = owner.GetViewport().GetVisibleRect().Size.X;
		int popupWidth = Mathf.RoundToInt(Mathf.Clamp(viewportWidth - 32f, 280f, 420f));
		dialog.MinSize = new Vector2I(popupWidth, 180);

		if (returnToDashboard)
			dialog.Confirmed += () => ReturnToSafeDashboard(owner);

		owner.AddChild(dialog);
		dialog.PopupCentered();
		dialog.GrabFocus();
	}

	private static void ReturnToSafeDashboard(Node owner)
	{
		if (!owner.IsInsideTree())
			return;

		string path = UserSession.Role switch
		{
			UserRole.Student => "res://scene/Dashboard/UserDashboard/Student Dashboard/StudentDashboard.tscn",
			UserRole.Professor => "res://scene/Dashboard/UserDashboard/Professor Dashboard/ProfessorDashboard.tscn",
			UserRole.Admin => "res://scene/Dashboard/UserDashboard/Admin Dashboard/AdminDashboard.tscn",
			_ => "res://scene/UserSelection/UserSelection.tscn"
		};

		owner.GetTree().ChangeSceneToFile(path);
	}
}
