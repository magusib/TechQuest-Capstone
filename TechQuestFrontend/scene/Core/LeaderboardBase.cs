using Godot;
using System;
using System.Threading.Tasks;

public partial class LeaderboardBase : Control
{
	private const string LeaderboardUrl = "https://techquest-capstone.onrender.com/api/Leaderboard";
	private HttpRequest request;
	private string requestType = "";
	private Godot.Collections.Array currentRows = new();

	public override void _Ready()
	{
		request = new HttpRequest();
		AddChild(request);
		request.RequestCompleted += OnRequestCompleted;

		ConnectButton("LeaderboardPanel/MainVBox/Header/BackButton", OnBackPressed);
		ConnectButton("LeaderboardPanel/MainVBox/BottomButtons/PreviousRankingButton", OnPreviousPressed);
		ConnectButton("PreviousRankingPopup/PopupPanel/PopupVBox/CloseButton", OnPreviousClosePressed);

		Control resetButton = GetNodeOrNull<Control>("LeaderboardPanel/MainVBox/BottomButtons/ResetLeaderboardButton");
		if (resetButton != null)
		{
			resetButton.Visible = UserSession.IsAdmin;
			ConnectButton("LeaderboardPanel/MainVBox/BottomButtons/ResetLeaderboardButton", OnResetPressed);
		}

		Control resetPopup = GetNodeOrNull<Control>("ResetConfirmationPopup");
		if (resetPopup != null)
		{
			resetPopup.Visible = false;
			ConnectButton("ResetConfirmationPopup/PopupPanel/PopupVBox/Buttons/CancelButton", OnResetCancelPressed);
			ConnectButton("ResetConfirmationPopup/PopupPanel/PopupVBox/Buttons/ConfirmButton", OnResetConfirmPressed);
		}

		if (!UserSession.IsLoggedIn)
		{
			GetTree().ChangeSceneToFile(GetBackScene());
			return;
		}

		LoadCurrentRanking();
	}

	protected virtual string GetBackScene()
	{
		return UserSession.Role switch
		{
			UserRole.Student => "res://scene/Dashboard/UserDashboard/Student Dashboard/StudentDashboard.tscn",
			UserRole.Professor => "res://scene/Dashboard/UserDashboard/Professor Dashboard/ProfessorDashboard.tscn",
			_ => "res://scene/Dashboard/UserDashboard/Admin Dashboard/AdminDashboard.tscn"
		};
	}

	private void ConnectButton(string path, Action callback)
	{
		BaseButton button = GetNodeOrNull<BaseButton>(path);
		if (button != null)
			button.Pressed += callback;
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile(GetBackScene());
	}

	private void OnPreviousPressed()
	{
		Control popup = GetNodeOrNull<Control>("PreviousRankingPopup");
		if (popup != null)
			popup.Visible = true;

		SendRequest("previous", HttpClient.Method.Get, "/previous");
	}

	private void OnPreviousClosePressed()
	{
		Control popup = GetNodeOrNull<Control>("PreviousRankingPopup");
		if (popup != null)
			popup.Visible = false;
	}

	private void OnResetPressed()
	{
		Control popup = GetNodeOrNull<Control>("ResetConfirmationPopup");
		if (popup != null)
			popup.Visible = true;
	}

	private void OnResetCancelPressed()
	{
		Control popup = GetNodeOrNull<Control>("ResetConfirmationPopup");
		if (popup != null)
			popup.Visible = false;
	}

	private void OnResetConfirmPressed()
	{
		SendRequest("reset", HttpClient.Method.Post, "/reset");
	}

	private void LoadCurrentRanking()
	{
		SendRequest("current", HttpClient.Method.Get, "");
	}

	private void SendRequest(string type, HttpClient.Method method, string endpoint)
	{
		if (string.IsNullOrWhiteSpace(UserSession.Token))
			return;

		requestType = type;
		string[] headers = { "Authorization: Bearer " + UserSession.Token };
		Error error = request.Request(LeaderboardUrl + endpoint, headers, method);
		if (error != Error.Ok)
			GD.PrintErr("Leaderboard request could not start: " + error);
	}

	private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		string responseText = System.Text.Encoding.UTF8.GetString(body);
		string completedType = requestType;
		requestType = "";

		if (result != (long)HttpRequest.Result.Success || responseCode < 200 || responseCode >= 300)
		{
			GD.PrintErr("Leaderboard request failed: " + responseCode + " " + responseText);
			return;
		}

		Variant parsed = Json.ParseString(responseText);
		if (completedType == "current" && parsed.VariantType == Variant.Type.Array)
		{
			currentRows = parsed.AsGodotArray();
			DisplayCurrentRanking();
		}
		else if (completedType == "previous")
		{
			DisplayPreviousRanking(parsed);
		}
		else if (completedType == "reset")
		{
			Control popup = GetNodeOrNull<Control>("ResetConfirmationPopup");
			if (popup != null)
				popup.Visible = false;
			LoadCurrentRanking();
		}
	}

	private void DisplayCurrentRanking()
	{
		for (int rank = 1; rank <= 3; rank++)
		{
			if (rank <= currentRows.Count)
				DisplayPlace(rank, currentRows[rank - 1].AsGodotDictionary());
			else
				DisplayEmptyPlace(rank);
		}

		VBoxContainer rankingList = GetNodeOrNull<VBoxContainer>("LeaderboardPanel/MainVBox/RankingScroll/RankingList");
		if (rankingList == null)
			return;

		foreach (Node child in rankingList.GetChildren())
			child.QueueFree();

		for (int index = 3; index < currentRows.Count; index++)
			AddRankingRow(rankingList, currentRows[index].AsGodotDictionary());
	}

	private void DisplayPlace(int rank, Godot.Collections.Dictionary student)
	{
		string placeName = rank switch
		{
			1 => "FirstPlace",
			2 => "SecondPlace",
			_ => "ThirdPlace"
		};
		string basePath = "LeaderboardPanel/MainVBox/Top3Panel/" + placeName + "/CardVBox/";
		Label rankLabel = GetNodeOrNull<Label>(basePath + "RankLabel");
		Label nameLabel = GetNodeOrNull<Label>(basePath + "NameLabel");
		Label pointsLabel = GetNodeOrNull<Label>(basePath + "PointsLabel");
		TextureRect avatar = GetNodeOrNull<TextureRect>(basePath + "AvatarCircle");

		if (rankLabel != null)
			rankLabel.Text = GetRankText(rank);
		if (nameLabel != null)
			nameLabel.Text = GetName(student);
		if (pointsLabel != null)
			pointsLabel.Text = GetInt(student, "totalPoints") + " POINTS";
		_ = SetAvatarAsync(avatar, GetString(student, "avatar"));
	}

	private void DisplayEmptyPlace(int rank)
	{
		string placeName = rank switch
		{
			1 => "FirstPlace",
			2 => "SecondPlace",
			_ => "ThirdPlace"
		};
		string basePath = "LeaderboardPanel/MainVBox/Top3Panel/" + placeName + "/CardVBox/";
		Label nameLabel = GetNodeOrNull<Label>(basePath + "NameLabel");
		Label pointsLabel = GetNodeOrNull<Label>(basePath + "PointsLabel");
		TextureRect avatar = GetNodeOrNull<TextureRect>(basePath + "AvatarCircle");
		if (nameLabel != null)
			nameLabel.Text = "No student";
		if (pointsLabel != null)
			pointsLabel.Text = "0 POINTS";
		if (avatar != null)
			avatar.Texture = null;
	}

	private void AddRankingRow(VBoxContainer rankingList, Godot.Collections.Dictionary student)
	{
		HBoxContainer row = new HBoxContainer();
		row.CustomMinimumSize = new Vector2(0, 42);
		row.AddChild(CreateRowLabel(GetInt(student, "rankPosition").ToString(), 100));
		row.AddChild(CreateRowLabel(GetName(student), 459));
		int yearLevel = GetInt(student, "yearLevel");
		row.AddChild(CreateRowLabel(yearLevel > 0 ? "Year " + yearLevel : "N/A", 200));
		row.AddChild(CreateRowLabel(GetInt(student, "totalPoints").ToString(), 200));
		rankingList.AddChild(row);
	}

	private Label CreateRowLabel(string text, float minimumWidth)
	{
		Label label = new Label();
		label.Text = text;
		label.CustomMinimumSize = new Vector2(minimumWidth, 42);
		label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Center;
		return label;
	}

	private void DisplayPreviousRanking(Variant parsed)
	{
		Label first = GetNodeOrNull<Label>("PreviousRankingPopup/PopupPanel/PopupVBox/PreviousTop3/First");
		Label second = GetNodeOrNull<Label>("PreviousRankingPopup/PopupPanel/PopupVBox/PreviousTop3/Second");
		Label third = GetNodeOrNull<Label>("PreviousRankingPopup/PopupPanel/PopupVBox/PreviousTop3/Third");
		Label info = GetNodeOrNull<Label>("PreviousRankingPopup/PopupPanel/PopupVBox/InfoLabel");
		if (parsed.VariantType != Variant.Type.Dictionary)
		{
			SetPreviousLabel(first, 1, null);
			SetPreviousLabel(second, 2, null);
			SetPreviousLabel(third, 3, null);
			if (info != null)
				info.Text = "No previous ranking yet.";
			return;
		}

		Godot.Collections.Dictionary history = parsed.AsGodotDictionary();
		if (info != null)
			info.Text = "Top 3 before the reset on " + GetString(history, "resetAt");
		Godot.Collections.Array entries = history["entries"].AsGodotArray();
		for (int rank = 1; rank <= 3; rank++)
		{
			Godot.Collections.Dictionary student = null;
			if (rank <= entries.Count)
				student = entries[rank - 1].AsGodotDictionary();
			Label label = rank switch { 1 => first, 2 => second, _ => third };
			SetPreviousLabel(label, rank, student);
		}
	}

	private void SetPreviousLabel(Label label, int rank, Godot.Collections.Dictionary student)
	{
		if (label == null)
			return;
		label.Text = student == null
			? GetRankText(rank) + "  No student"
			: GetRankText(rank) + "  " + GetName(student) + "  -  " + GetInt(student, "totalPoints") + " Points";
	}

	private string GetName(Godot.Collections.Dictionary student)
	{
		return (GetString(student, "firstName") + " " + GetString(student, "lastName")).Trim();
	}

	private string GetRankText(int rank)
	{
		return rank switch { 1 => "1ST PLACE", 2 => "2ND PLACE", _ => "3RD PLACE" };
	}

	private string GetString(Godot.Collections.Dictionary values, string key)
	{
		if (!values.ContainsKey(key) || values[key].VariantType == Variant.Type.Nil)
			return "";
		return values[key].AsString();
	}

	private int GetInt(Godot.Collections.Dictionary values, string key)
	{
		if (!values.ContainsKey(key))
			return 0;
		Variant value = values[key];
		return value.VariantType switch
		{
			Variant.Type.Int => value.AsInt32(),
			Variant.Type.Float => (int)value.AsDouble(),
			Variant.Type.String => int.TryParse(value.AsString(), out int parsed) ? parsed : 0,
			_ => 0
		};
	}

	private async Task SetAvatarAsync(TextureRect display, string avatarPath)
	{
		if (display == null || string.IsNullOrWhiteSpace(avatarPath))
			return;

		try
		{
			Texture2D texture = await AccountApiClient.LoadAvatarAsync(avatarPath);
			if (IsInstanceValid(display) && texture != null)
				display.Texture = texture;
		}
		catch (Exception error)
		{
			GD.PrintErr($"Leaderboard avatar load failed: {error.Message}");
		}
	}
}
