namespace TechQuest.Core;

/// <summary>
/// Centralized scene paths used by dashboard navigation.
/// Update a path only when the corresponding real scene is created.
/// </summary>
public static class DashboardNavigationPaths
{
	public const string UserSelection =
		"res://scene/UserSelection/UserSelection.tscn";

	public const string StudentDashboard =
		"res://scene/Dashboard/UserDashboard/Student Dashboard/StudentDashboard.tscn";

	public const string ProfessorDashboard =
		"res://scene/Dashboard/UserDashboard/Professor Dashboard/ProfessorDashboard.tscn";

	public const string AdminDashboard =
		"res://scene/Dashboard/UserDashboard/Admin Dashboard/AdminDashboard.tscn";

	public const string StudentProfile =
		"res://scene/Dashboard/UserDashboard/Student Dashboard/StudentProfile.tscn";

	public const string ProfessorProfile =
		"res://scene/Dashboard/UserDashboard/Professor Dashboard/ProfessorProfile.tscn";

	public const string AdminProfile =
		"res://scene/Dashboard/UserDashboard/Admin Dashboard/AdminProfile.tscn";

	public const string Room =
		"res://scene/Dashboard/Modules/Room/Room.tscn";

	public const string Leaderboard =
		"res://scene/Dashboard/Modules/Leaderboard/Leaderboard.tscn";

	public const string Shop =
		"res://scene/Dashboard/Modules/Shop/Shop.tscn";

	public const string Character =
		"res://scene/Dashboard/Modules/Character/StudentCharacter.tscn";

	public const string Storyline =
		"res://scene/Dashboard/Modules/Storyline/Storyline.tscn";

	public const string EasyMap =
		"res://scene/AdventureGame/Difficulty/EasyLevelMap/EasyMap.tscn";

	public const string MediumMap =
		"res://scene/AdventureGame/Difficulty/MediumLevelMap/MediumMap.tscn";

	public const string HardMap =
		"res://scene/AdventureGame/Difficulty/HardLevelMap/HardMap.tscn";
}
