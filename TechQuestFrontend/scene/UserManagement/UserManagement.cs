using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class UserManagement : Control
{
	private Button backButton;
	private Button professorButton;
	private LineEdit searchInput;
	private OptionButton sortYearLevelButton;
	private VBoxContainer studentList;
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
		searchInput = GetNode<LineEdit>("MarginContainer/MainVBox/StudentPanel/StudentContent/StudentToolbar/SearchStudentInput");
		sortYearLevelButton = GetNode<OptionButton>("MarginContainer/MainVBox/StudentPanel/StudentContent/StudentToolbar/SortYearLevelButton");
		studentList = GetNode<VBoxContainer>("MarginContainer/MainVBox/StudentPanel/StudentContent/StudentScrollContainer/StudentList");
		deleteConfirmationDialog = GetNode<ConfirmationDialog>("DeleteConfirmationDialog");
		messageDialog = new AcceptDialog { Title = "User Management" };
		AddChild(messageDialog);

		sortYearLevelButton.AddItem("Year level: low to high");
		sortYearLevelButton.AddItem("Year level: high to low");
		backButton.Pressed += OnBackPressed;
		professorButton.Pressed += OnProfessorPressed;
		searchInput.TextChanged += OnSearchChanged;
		sortYearLevelButton.ItemSelected += OnSortChanged;
		deleteConfirmationDialog.Confirmed += OnDeleteConfirmed;
		_ = LoadStudentsAsync();

	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Dashboard/UserDashboard/Admin Dashboard/AdminDashboard.tscn");
	}
	
	private void OnProfessorPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/UserManagement/ProfessorManagement.tscn");
	}

	private async Task LoadStudentsAsync()
	{
		try
		{
			users = await AccountApiClient.GetUsersAsync();
			RenderStudents();
		}
		catch (Exception exception)
		{
			ShowMessage("Could not load students: " + exception.Message);
		}
	}

	private void OnSearchChanged(string searchText)
	{
		RenderStudents();
	}

	private void OnSortChanged(long selectedIndex)
	{
		RenderStudents();
	}

	private void RenderStudents()
	{
		ClearRows(studentList);
		string searchText = searchInput.Text.Trim();
		var matches = users
			.Where(user => string.Equals(user.Role, "student", StringComparison.OrdinalIgnoreCase))
			.Where(user => string.IsNullOrWhiteSpace(searchText)
				|| user.FullName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
				|| user.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase));

		matches = sortYearLevelButton.Selected == 1
			? matches.OrderByDescending(user => user.YearLevel ?? 0).ThenBy(user => user.FullName, StringComparer.OrdinalIgnoreCase)
			: matches.OrderBy(user => user.YearLevel ?? 0).ThenBy(user => user.FullName, StringComparer.OrdinalIgnoreCase);

		var studentMatches = matches.ToList();
		if (studentMatches.Count == 0)
		{
			studentList.AddChild(new Label { Text = "No students found." });
			return;
		}

		foreach (ManagedUserDto user in studentMatches)
		{
			var row = new HBoxContainer { CustomMinimumSize = new Vector2(0, 42) };
			row.AddChild(CreateCell(user.FullName, 210));
			row.AddChild(CreateCell(user.Email, 330, true));
			row.AddChild(CreateCell(user.YearLevel.HasValue ? $"Year {user.YearLevel.Value}" : "N/A", 140));
			var deleteButton = new Button
			{
				Text = "🗑",
				TooltipText = "Delete student",
				CustomMinimumSize = new Vector2(90, 40)
			};
			deleteButton.Pressed += () => AskToDelete(user);
			row.AddChild(deleteButton);
			studentList.AddChild(row);
		}
	}

	private void AskToDelete(ManagedUserDto user)
	{
		pendingDeleteUserId = user.Id;
		deleteConfirmationDialog.DialogText = $"Delete student {user.FullName}? This cannot be undone.";
		deleteConfirmationDialog.PopupCentered();
	}

	private async void OnDeleteConfirmed()
	{
		try
		{
			await AccountApiClient.DeleteUserAsync(pendingDeleteUserId);
			users.RemoveAll(user => user.Id == pendingDeleteUserId);
			RenderStudents();
		}
		catch (Exception exception)
		{
			ShowMessage("Could not delete student: " + exception.Message);
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
