using Godot;
using System;
using System.Text;

public partial class AdminLogin : Control
{
	private const string API_BASE_URL =
		"http://localhost:5000/api/Auth";

	private HttpRequest httpRequest;
	private Panel loginPanel;

	private LineEdit loginEmailInput;
	private LineEdit loginPasswordInput;

	private Button loginButton;
	private Button backButton;
	private Button forgotPasswordLink;

	private Window forgotPasswordPopup;
	private LineEdit forgotEmailInput;
	private LineEdit newPasswordInput;
	private LineEdit confirmNewPasswordInput;
	private Button forgotSaveButton;
	private Button forgotCancelButton;

	private Window otpPopup;
	private LineEdit otpInput;
	private Button otpConfirmButton;
	private Button otpResendButton;
	private Button otpCancelButton;

	private AcceptDialog messageDialog;

	private string pendingEmail = "";
	private bool isForgotPasswordOTP = false;
	private int otpAttempts = 0;
	private const int MaxOTPAttempts = 3;

	public override void _Ready()
	{
		httpRequest = GetNode<HttpRequest>("HTTPRequest");
		loginPanel = GetNode<Panel>("LoginPanel");

		loginEmailInput = GetNode<LineEdit>("LoginPanel/EmailInput");
		loginPasswordInput = GetNode<LineEdit>("LoginPanel/PasswordInput");

		loginButton = GetNode<Button>("LoginPanel/LoginButton");
		backButton = GetNode<Button>("LoginPanel/Back");
		forgotPasswordLink = GetNode<Button>("LoginPanel/ForgotPasswordLink");

		forgotPasswordPopup = GetNode<Window>("ForgotPasswordPopup");
		forgotEmailInput = GetNode<LineEdit>("ForgotPasswordPopup/EmailInput");
		newPasswordInput = GetNode<LineEdit>("ForgotPasswordPopup/NewPasswordInput");
		confirmNewPasswordInput = GetNode<LineEdit>("ForgotPasswordPopup/ConfirmPasswordInput");
		forgotSaveButton = GetNode<Button>("ForgotPasswordPopup/SaveButton");
		forgotCancelButton = GetNode<Button>("ForgotPasswordPopup/CancelButton");

		otpPopup = GetNode<Window>("OTPPopup");
		otpInput = GetNode<LineEdit>("OTPPopup/OTPInput");
		otpConfirmButton = GetNode<Button>("OTPPopup/ConfirmButton");
		otpResendButton = GetNode<Button>("OTPPopup/ResendButton");
		otpCancelButton = GetNode<Button>("OTPPopup/CancelButton");

		messageDialog = GetNode<AcceptDialog>("MessageDialog");

		forgotPasswordPopup.Visible = false;
		otpPopup.Visible = false;

		loginButton.Pressed += OnLoginPressed;
		backButton.Pressed += OnBackPressed;
		forgotPasswordLink.Pressed += OnForgotPasswordPressed;
		forgotSaveButton.Pressed += OnForgotPasswordSavePressed;
		forgotCancelButton.Pressed += OnForgotPasswordCancelPressed;
		otpConfirmButton.Pressed += OnOTPConfirmPressed;
		otpResendButton.Pressed += OnOTPResendPressed;
		otpCancelButton.Pressed += OnOTPCancelPressed;

		httpRequest.RequestCompleted += OnRequestCompleted;
	}

	private void OnLoginPressed()
	{
		string email = loginEmailInput.Text.Trim();
		string password = loginPasswordInput.Text;

		if (string.IsNullOrWhiteSpace(email))
		{
			ShowMessage("Please enter your email.");
			return;
		}

		if (!IsPTCEmail(email))
		{
			ShowMessage("Please use your PTC institutional email.");
			return;
		}

		if (string.IsNullOrWhiteSpace(password))
		{
			ShowMessage("Please enter your password.");
			return;
		}

		SendLoginRequest(email, password);
	}

	private void SendLoginRequest(string email, string password)
	{
		var data = new Godot.Collections.Dictionary
		{
			{ "email", email },
			{ "password", password }
		};

		SendPostRequest("/login/admin", data);
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/UserSelection/UserSelection.tscn");
	}

	private void OnForgotPasswordPressed()
	{
		forgotEmailInput.Clear();
		newPasswordInput.Clear();
		confirmNewPasswordInput.Clear();
		OpenForgotPasswordPopup();
	}

	private void OpenForgotPasswordPopup()
	{
		forgotPasswordPopup.Visible = true;
		forgotPasswordPopup.Popup();
		forgotPasswordPopup.PopupCentered();
		forgotPasswordPopup.GrabFocus();
	}

	private void OpenOTPPopup()
	{
		otpPopup.Visible = true;
		otpPopup.Popup();
		otpPopup.PopupCentered();
		otpPopup.GrabFocus();
	}

	private void SetForgotPasswordBusy(bool busy)
	{
		forgotSaveButton.Disabled = busy;
		forgotCancelButton.Disabled = busy;
		forgotSaveButton.Text = busy ? "Sending..." : "Save";
	}

	private void SetOTPBusy(bool busy)
	{
		otpConfirmButton.Disabled = busy;
		otpResendButton.Disabled = busy;
		otpCancelButton.Disabled = busy;
		otpConfirmButton.Text = busy ? "Sending..." : "Confirm";
	}

	private void OnForgotPasswordSavePressed()
	{
		string email = forgotEmailInput.Text.Trim();
		string newPassword = newPasswordInput.Text;
		string confirmPassword = confirmNewPasswordInput.Text;

		if (string.IsNullOrWhiteSpace(email))
		{
			ShowMessage("Please enter your email.");
			return;
		}

		if (!IsPTCEmail(email))
		{
			ShowMessage("Please use your PTC institutional email.");
			return;
		}

		if (string.IsNullOrWhiteSpace(newPassword))
		{
			ShowMessage("Please enter your new password.");
			return;
		}

		if (string.IsNullOrWhiteSpace(confirmPassword))
		{
			ShowMessage("Please confirm your new password.");
			return;
		}

		if (newPassword != confirmPassword)
		{
			ShowMessage("Passwords do not match.");
			return;
		}

		otpAttempts = 0;
		isForgotPasswordOTP = true;
		pendingEmail = email;
		SetForgotPasswordBusy(true);

		var data = new Godot.Collections.Dictionary
		{
			{ "email", email },
			{ "newPassword", newPassword },
			{ "confirmPassword", confirmPassword }
		};

		SendPostRequest("/forgot-password", data);
	}

	private void OnForgotPasswordCancelPressed()
	{
		forgotPasswordPopup.Hide();
	}

	private void OnOTPConfirmPressed()
	{
		string otp = otpInput.Text.Trim();

		if (string.IsNullOrWhiteSpace(otp))
		{
			ShowMessage("Please enter the OTP.");
			return;
		}

		if (otp.Length != 6 || !IsDigitsOnly(otp))
		{
			ShowMessage("OTP must contain exactly 6 digits.");
			return;
		}

		if (otpAttempts >= MaxOTPAttempts)
		{
			ShowMessage("Maximum OTP attempts reached. Please resend a new OTP.");
			return;
		}

		otpAttempts++;

		var data = new Godot.Collections.Dictionary
		{
			{ "email", pendingEmail },
			{ "otp", otp }
		};

		SendPostRequest("/verify-forgot-password-otp", data);
	}

	private void OnOTPResendPressed()
	{
		if (string.IsNullOrWhiteSpace(pendingEmail))
		{
			ShowMessage("Email information is missing.");
			return;
		}

		otpInput.Clear();
		otpAttempts = 0;

		var data = new Godot.Collections.Dictionary
		{
			{ "email", pendingEmail }
		};

		SendPostRequest("/resend-forgot-password-otp", data);
	}

	private void OnOTPCancelPressed()
	{
		otpPopup.Hide();
		otpInput.Clear();
		otpAttempts = 0;
	}

	private void SendPostRequest(string endpoint, Godot.Collections.Dictionary data)
	{
		string json = Json.Stringify(data);
		string[] headers = { "Content-Type: application/json" };

		Error error = httpRequest.Request(
			API_BASE_URL + endpoint,
			headers,
			HttpClient.Method.Post,
			json
		);

		if (error != Error.Ok)
		{
			ShowMessage("Unable to connect to TechQuestBackend.");
			GD.PrintErr($"HTTPRequest Error: {error}");
		}
	}

	private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		string responseText = Encoding.UTF8.GetString(body);

		if (result != (long)HttpRequest.Result.Success)
		{
			ShowMessage("Could not connect to TechQuestBackend.");
			return;
		}

		if (responseCode >= 200 && responseCode < 300)
		{
			HandleSuccessfulResponse(responseText);
			return;
		}

		ShowMessage(GetApiErrorMessage(responseText));
	}

	private void HandleSuccessfulResponse(string responseText)
	{
		if (responseText.Contains("login", StringComparison.OrdinalIgnoreCase))
		{
			StoreSession(responseText);
			ShowMessage("Login successful!");
			NavigateToRoleDashboard();
			return;
		}

		if (isForgotPasswordOTP && !otpPopup.Visible)
		{
			forgotPasswordPopup.Hide();
			otpInput.Clear();
			OpenOTPPopup();
			SetOTPBusy(false);
			SetForgotPasswordBusy(false);
			ShowMessage("Password reset OTP has been sent to your email.");
			return;
		}

		if (isForgotPasswordOTP && otpPopup.Visible)
		{
			otpPopup.Hide();
			otpInput.Clear();
			ShowMessage("Password reset successful!");
			loginEmailInput.Text = pendingEmail;
			loginPasswordInput.Clear();
			return;
		}
	}

	private void StoreSession(string responseText)
	{
		var response = Json.ParseString(responseText).AsGodotDictionary();
		int userId = response.ContainsKey("userId") ? (int)response["userId"] : 0;
		string role = response.ContainsKey("role") ? response["role"].ToString() : "admin";
		string firstName = response.ContainsKey("firstName") ? response["firstName"].ToString() : "";
		string lastName = response.ContainsKey("lastName") ? response["lastName"].ToString() : "";
		string avatar = response.ContainsKey("avatar") ? response["avatar"].ToString() : "";
		AccountSession.Current?.SetAccount(userId, role, firstName, lastName, 0, 0, avatar);
	}

	private void NavigateToRoleDashboard()
	{
		string dashboardPath = AccountSession.Current?.Role switch
		{
			"student" => "res://scene/Dashboard/UserDashboard/Student Dashboard/StudentDashboard.tscn",
			"professor" => "res://scene/Dashboard/UserDashboard/Professor Dashboard/ProfessorDashboard.tscn",
			"admin" => "res://scene/Dashboard/UserDashboard/Admin Dashboard/AdminDashboard.tscn",
			_ => ""
		};

		if (string.IsNullOrWhiteSpace(dashboardPath))
		{
			ShowMessage("The account role is invalid.");
			return;
		}

		GetTree().ChangeSceneToFile(dashboardPath);
	}

	private string GetApiErrorMessage(string responseText)
	{
		if (string.IsNullOrWhiteSpace(responseText))
		{
			return "The server returned an error.";
		}

		if (responseText.Length > 500)
		{
			return "The server returned an unexpected error.";
		}

		return responseText;
	}

	private bool IsPTCEmail(string email)
	{
		return email.EndsWith("@paterostechnologicalcollege.edu.ph", StringComparison.OrdinalIgnoreCase);
	}

	private bool IsDigitsOnly(string text)
	{
		foreach (char character in text)
		{
			if (!char.IsDigit(character))
			{
				return false;
			}
		}

		return true;
	}

	private void ShowMessage(string message)
	{
		messageDialog.DialogText = message;
		messageDialog.Visible = true;
		messageDialog.PopupCentered();
		messageDialog.GrabFocus();
	}
}
