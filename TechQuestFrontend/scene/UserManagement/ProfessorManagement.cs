using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class ProfessorManagement : Control
{
	private Button backButton;
	private Button professorButton;
	private Button studentButton;
	private Button registerButton;
	private LineEdit searchInput;
	private VBoxContainer professorList;
	private ConfirmationDialog deleteConfirmationDialog;
	private AcceptDialog messageDialog;
	private List<ManagedUserDto> users = new();
	private int pendingDeleteUserId;

	public override void _Ready()
	{
		if (!UserSession.IsAdmin)
		{
			GetTree().ChangeSceneToFile("res://scene/Dashboard/UserDashboard/Admin Dashboard/AdminDashboard.tscn");
			return;
		}

		backButton = GetNode<Button>("MarginContainer/MainVBox/TopBar/Back");
		professorButton = GetNode<Button>("MarginContainer/MainVBox/UserTypeContainer/ProfessorButton");
		studentButton = GetNode<Button>("MarginContainer/MainVBox/UserTypeContainer/StudentButton");
		registerButton = GetNode<Button>("MarginContainer/MainVBox/UserTypeContainer/RegisterButton");
		searchInput = GetNode<LineEdit>("MarginContainer/MainVBox/ProfessorPanel/ProfessorContent/ProfessorToolbar/SearchProfessorInput");
		professorList = GetNode<VBoxContainer>("MarginContainer/MainVBox/ProfessorPanel/ProfessorContent/ProfessorScrollContainer/ProfessorList");
		deleteConfirmationDialog = GetNode<ConfirmationDialog>("DeleteConfirmationDialog");
		messageDialog = new AcceptDialog { Title = "User Management" };
		AddChild(messageDialog);

		backButton.Pressed += OnBackPressed;
		professorButton.Pressed += OnProfessorPressed;
		studentButton.Pressed += OnStudentPressed;
		registerButton.Pressed += OnRegisterPressed;
		searchInput.TextChanged += OnSearchChanged;
		deleteConfirmationDialog.Confirmed += OnDeleteConfirmed;
		_ = LoadProfessorsAsync();

	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/UserDashboard/Admin Dashboard/AdminDashboard.tscn");
	}
	
	private void OnProfessorPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/UserManagement/ProfessorManagement.tscn");
	}
	
	private void OnStudentPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/UserManagement/UserManagement.tscn");
	}
	
	private void OnRegisterPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/UserManagement/ProfessorRegister.tscn");
	}

	private async Task LoadProfessorsAsync()
	{
		try
		{
			users = await AccountApiClient.GetUsersAsync();
			RenderProfessors();
		}
		catch (Exception exception)
		{
			ShowMessage("Could not load professors: " + exception.Message);
		}
	}

	private void OnSearchChanged(string searchText)
	{
		RenderProfessors();
	}

	private void RenderProfessors()
	{
		ClearRows(professorList);
		string searchText = searchInput.Text.Trim();
		var matches = users
			.Where(user => string.Equals(user.Role, "professor", StringComparison.OrdinalIgnoreCase))
			.Where(user => string.IsNullOrWhiteSpace(searchText)
				|| user.FullName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
				|| user.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase))
			.OrderBy(user => user.FullName, StringComparer.OrdinalIgnoreCase)
			.ToList();

		if (matches.Count == 0)
		{
			professorList.AddChild(new Label { Text = "No professors found." });
			return;
		}

		foreach (ManagedUserDto user in matches)
		{
			var row = new HBoxContainer { CustomMinimumSize = new Vector2(0, 42) };
			row.AddChild(CreateCell(user.FullName, 210));
			row.AddChild(CreateCell(user.Email, 330, true));
			var deleteButton = new Button
			{
				Text = "🗑",
				TooltipText = "Delete professor",
				CustomMinimumSize = new Vector2(90, 40)
			};
			deleteButton.Pressed += () => AskToDelete(user);
			row.AddChild(deleteButton);
			professorList.AddChild(row);
		}
	}

	private void AskToDelete(ManagedUserDto user)
	{
		pendingDeleteUserId = user.Id;
		deleteConfirmationDialog.DialogText = $"Delete professor {user.FullName}? This cannot be undone.";
		deleteConfirmationDialog.PopupCentered();
	}

	private async void OnDeleteConfirmed()
	{
		try
		{
			await AccountApiClient.DeleteUserAsync(pendingDeleteUserId);
			users.RemoveAll(user => user.Id == pendingDeleteUserId);
			RenderProfessors();
		}
		catch (Exception exception)
		{
			ShowMessage("Could not delete professor: " + exception.Message);
		}
	}

	private void ShowMessage(string message)
	{
		messageDialog.DialogText = message;
		messageDialog.PopupCentered();
	}

	private static void ClearRows(VBoxContainer list)
	{
		foreach (Node child in list.GetChildren())
		{
			list.RemoveChild(child);
			child.QueueFree();
		}
	}

	private static Label CreateCell(string text, float width, bool expand = false)
	{
		return new Label
		{
			Text = text,
			CustomMinimumSize = new Vector2(width, 0),
			SizeFlagsHorizontal = expand ? Control.SizeFlags.ExpandFill : Control.SizeFlags.ShrinkBegin,
			VerticalAlignment = VerticalAlignment.Center
		};
	}
}
