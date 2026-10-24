using Godot;
using System;

public partial class UserProfileBase : Control
{
	private TextureRect avatarHolder;
	private Button backButton;
	private Button uploadButton;
	private Label firstNameLabel;
	private Label lastNameLabel;
	private Label emailLabel;
	private Label yearLevelLabel;

	protected virtual string DashboardScenePath => "";

	public override void _Ready()
	{
		avatarHolder = GetNodeOrNull<TextureRect>("MainPanel/AvatarHolder");
		backButton = GetNodeOrNull<Button>("MainPanel/BackButton");
		uploadButton = GetNodeOrNull<Button>("MainPanel/UploadButton");
		firstNameLabel = GetNodeOrNull<Label>("MainPanel/InformationPanel/FirstnameLabel");
		lastNameLabel = GetNodeOrNull<Label>("MainPanel/InformationPanel/LastnameLabel");
		emailLabel = GetNodeOrNull<Label>("MainPanel/InformationPanel/EmailLabel");
		yearLevelLabel = GetNodeOrNull<Label>("MainPanel/InformationPanel/YearLevelLabel");

		if (backButton != null)
			backButton.Pressed += OnBackPressed;

		if (yearLevelLabel != null)
			yearLevelLabel.Visible = UserSession.IsStudent;

		if (UserSession.IsStudent && avatarHolder != null)
		{
			avatarHolder.MouseFilter = Control.MouseFilterEnum.Stop;
			avatarHolder.GuiInput += OnAvatarInput;
		}

		if (uploadButton != null)
		{
			uploadButton.Visible = UserSession.IsAdmin || UserSession.IsProfessor;
			uploadButton.Pressed += OpenAvatarPicker;
		}

		LoadProfile();
	}

	private async void LoadProfile()
	{
		try
		{
			AccountProfileDto profile = await AccountApiClient.GetProfileAsync();
			if (firstNameLabel != null) firstNameLabel.Text = profile.FirstName;
			if (lastNameLabel != null) lastNameLabel.Text = profile.LastName;
			if (emailLabel != null) emailLabel.Text = profile.Email;
			if (yearLevelLabel != null)
				yearLevelLabel.Text = profile.YearLevel?.ToString() ?? "Not set";

			UserSession.UpdateYearLevel(profile.YearLevel);
			UserSession.UpdateAvatar(profile.Avatar);
			if (avatarHolder != null)
				avatarHolder.Texture = await AccountApiClient.LoadAvatarAsync(profile.Avatar);
		}
		catch (Exception error)
		{
			GD.PrintErr("Profile load failed: " + error.Message);
		}
	}

	private void OnAvatarInput(InputEvent input)
	{
		if (input is InputEventMouseButton mouseEvent && mouseEvent.Pressed
			&& mouseEvent.ButtonIndex == MouseButton.Left)
			GetTree().ChangeSceneToFile("res://scene/Dashboard/Modules/Character/StudentAvatar.tscn");
	}

	private void OpenAvatarPicker()
	{
		if (!UserSession.IsAdmin && !UserSession.IsProfessor)
			return;

		FileDialog dialog = new()
		{
			Access = FileDialog.AccessEnum.Filesystem,
			FileMode = FileDialog.FileModeEnum.OpenFile,
			Filters = new[] { "*.png ; PNG", "*.jpg ; JPG", "*.jpeg ; JPEG" }
		};
		dialog.FileSelected += path =>
		{
			dialog.QueueFree();
			UploadAvatar(path);
		};
		AddChild(dialog);
		dialog.PopupCenteredRatio();
	}

	private async void UploadAvatar(string filePath)
	{
		try
		{
			string avatarPath = await AccountApiClient.UploadAvatarAsync(filePath);
			UserSession.UpdateAvatar(avatarPath);
			if (avatarHolder != null)
				avatarHolder.Texture = await AccountApiClient.LoadAvatarAsync(avatarPath);
		}
		catch (Exception error)
		{
			GD.PrintErr("Avatar upload failed: " + error.Message);
		}
	}

	private void OnBackPressed()
	{
		if (GetParent() is DashboardBase)
		{
			Hide();
			return;
		}

		if (!string.IsNullOrWhiteSpace(DashboardScenePath))
			GetTree().ChangeSceneToFile(DashboardScenePath);
	}
}
