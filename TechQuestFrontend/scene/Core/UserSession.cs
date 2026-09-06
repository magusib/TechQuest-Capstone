using Godot;
using System;

public enum UserRole
{
	None,
	Student,
	Professor,
	Admin
}

/// <summary>
/// Stores the currently authenticated user's session data while Godot
/// changes scenes.
/// </summary>
public static class UserSession
{
	public static UserRole Role { get; private set; } = UserRole.None;

	public static int UserId { get; private set; }
	public static string FirstName { get; private set; } = "";
	public static string LastName { get; private set; } = "";
	public static string Email { get; private set; } = "";
	public static string Token { get; private set; } = "";

	public static int Coins { get; private set; }
	public static int Points { get; private set; }

	public static string AvatarPath { get; private set; } = "";

	public static string FullName
	{
		get
		{
			string name = $"{FirstName} {LastName}".Trim();
			return string.IsNullOrWhiteSpace(name) ? "User" : name;
		}
	}

	public static bool IsLoggedIn => Role != UserRole.None;

	public static bool IsStudent => Role == UserRole.Student;
	public static bool IsProfessor => Role == UserRole.Professor;
	public static bool IsAdmin => Role == UserRole.Admin;

	public static void StartSession(
		UserRole role,
		int userId = 0,
		string firstName = "",
		string lastName = "",
		string email = "",
		int coins = 0,
		int points = 0,
		string avatarPath = "",
		string token = "")
	{
		Role = role;
		UserId = userId;
		FirstName = firstName ?? "";
		LastName = lastName ?? "";
		Email = email ?? "";
		Token = token ?? "";

		// Coins/points are meaningful only for Student.
		Coins = role == UserRole.Student ? Math.Max(0, coins) : 0;
		Points = role == UserRole.Student ? Math.Max(0, points) : 0;

		AvatarPath = avatarPath ?? "";

		GD.Print(
			$"UserSession started: Role={Role}, UserId={UserId}, " +
			$"Name={FullName}, Coins={Coins}, Points={Points}"
		);
	}

	public static bool TryStartFromLoginResponse(
		string responseText,
		UserRole expectedRole,
		out string errorMessage)
	{
		errorMessage = "";

		Godot.Variant parsed = Json.ParseString(responseText);

		if (parsed.VariantType != Variant.Type.Dictionary)
		{
			errorMessage = "Login response is not a valid JSON object.";
			return false;
		}

		var response = parsed.AsGodotDictionary();

		if (!TryGetRole(response, "role", out UserRole role))
		{
			errorMessage = "The account role is missing or invalid.";
			return false;
		}

		if (role != expectedRole)
		{
			errorMessage = "The account role does not match this login screen.";
			return false;
		}

		int userId = GetInt(response, "userId");

		if (userId <= 0)
		{
			errorMessage = "The login response is missing a valid user id.";
			return false;
		}

		string firstName = GetString(response, "firstName");
		string lastName = GetString(response, "lastName");
		string email = GetString(response, "email");
		string avatar = GetString(response, "avatar");
		string token = GetString(response, "token");

		int coins = role == UserRole.Student
			? GetInt(response, "totalCoins")
			: 0;

		int points = role == UserRole.Student
			? GetInt(response, "totalPoints")
			: 0;

		StartSession(
			role,
			userId,
			firstName,
			lastName,
			email,
			coins,
			points,
			avatar,
			token
		);

		return true;
	}

	public static void UpdateStudentProfile(
		int totalCoins,
		int totalPoints,
		string avatarPath = "")
	{
		if (!IsStudent)
			return;

		Coins = Math.Max(0, totalCoins);
		Points = Math.Max(0, totalPoints);

		if (!string.IsNullOrWhiteSpace(avatarPath))
			AvatarPath = avatarPath;
	}

	public static void UpdateProfile(
		string firstName,
		string lastName,
		string email,
		int coins,
		int points,
		string avatarPath = "")
	{
		FirstName = firstName ?? FirstName;
		LastName = lastName ?? LastName;
		Email = email ?? Email;

		if (IsStudent)
		{
			Coins = Math.Max(0, coins);
			Points = Math.Max(0, points);
		}

		if (!string.IsNullOrWhiteSpace(avatarPath))
			AvatarPath = avatarPath;
	}

	public static bool HasRole(UserRole role)
	{
		return Role == role;
	}

	private static bool TryGetRole(
		Godot.Collections.Dictionary response,
		string key,
		out UserRole role)
	{
		string roleText = GetString(response, key).Trim().ToLowerInvariant();

		role = roleText switch
		{
			"student" => UserRole.Student,
			"professor" => UserRole.Professor,
			"admin" => UserRole.Admin,
			_ => UserRole.None
		};

		return role != UserRole.None;
	}

	private static string GetString(
		Godot.Collections.Dictionary response,
		string key)
	{
		if (!response.ContainsKey(key))
			return "";

		Godot.Variant value = response[key];

		if (value.VariantType == Variant.Type.Nil)
			return "";

		return value.AsString();
	}

	private static int GetInt(
		Godot.Collections.Dictionary response,
		string key)
	{
		if (!response.ContainsKey(key))
			return 0;

		Godot.Variant value = response[key];

		return value.VariantType switch
		{
			Variant.Type.Int => value.AsInt32(),
			Variant.Type.Float => (int)value.AsDouble(),
			Variant.Type.String => int.TryParse(value.AsString(), out int parsed)
				? parsed
				: 0,
			_ => 0
		};
	}

	public static void Clear()
	{
		Role = UserRole.None;
		UserId = 0;
		FirstName = "";
		LastName = "";
		Email = "";
		Token = "";
		Coins = 0;
		Points = 0;
		AvatarPath = "";

		GD.Print("UserSession cleared.");
	}
}
