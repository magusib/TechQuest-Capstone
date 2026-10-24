using Godot;
using System;
using System.Text;

public partial class ProfessorRegister : Control
{
	private const string API_BASE_URL = "https://techquest-capstone.onrender.com/api/Auth";

	private HttpRequest httpRequest;
	private Panel registerPanel;

	private LineEdit firstNameInput;
	private LineEdit lastNameInput;
	private LineEdit emailInput;
	private LineEdit passwordInput;
	private LineEdit confirmPasswordInput;

	private Button registerButton;
	private Button backButton;
	private Button backToLoginButton;

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
	private bool isRegistrationOTP = false;
	private bool isForgotPasswordOTP = false;
	private int otpAttempts = 0;
	private const int MaxOTPAttempts = 3;

	public override void _Ready()
	{
		httpRequest = GetNode<HttpRequest>("HTTPRequest");
		registerPanel = GetNode<Panel>("RegisterPanel");

		firstNameInput = GetNode<LineEdit>("RegisterPanel/FirstNameInput");
		lastNameInput = GetNode<LineEdit>("RegisterPanel/LastNameInput");
		emailInput = GetNode<LineEdit>("RegisterPanel/EmailInput");
		passwordInput = GetNode<LineEdit>("RegisterPanel/PasswordInput");
		confirmPasswordInput = GetNode<LineEdit>("RegisterPanel/ConfirmPasswordInput");
		registerButton = GetNode<Button>("RegisterPanel/RegisterButton");
		backToLoginButton = GetNode<Button>("RegisterPanel/BackToLoginButton");

		backButton = GetNode<Button>("BackButton");

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

		registerButton.Pressed += OnRegisterPressed;
		backButton.Pressed += OnBackPressed;
		backToLoginButton.Pressed += OnBackToLoginPressed;
		forgotSaveButton.Pressed += OnForgotPasswordSavePressed;
		forgotCancelButton.Pressed += OnForgotPasswordCancelPressed;
		otpConfirmButton.Pressed += OnOTPConfirmPressed;
		otpResendButton.Pressed += OnOTPResendPressed;
		otpCancelButton.Pressed += OnOTPCancelPressed;

		httpRequest.RequestCompleted += OnRequestCompleted;
	}

	private void OnRegisterPressed()
	{
		string firstName = firstNameInput.Text.Trim();
		string lastName = lastNameInput.Text.Trim();
		string email = emailInput.Text.Trim();
		string password = passwordInput.Text;
		string confirmPassword = confirmPasswordInput.Text;

		if (string.IsNullOrWhiteSpace(firstName))
		{
			ShowMessage("Please enter your first name.");
			return;
		}

		if (string.IsNullOrWhiteSpace(lastName))
		{
			ShowMessage("Please enter your last name.");
			return;
		}

		if (string.IsNullOrWhiteSpace(email))
		{
			ShowMessage("Please enter your email.");
			return;
		}

		if (!IsPTCEmail(email))
		{
			ShowMessage("Only PTC institutional email is allowed.");
			return;
		}

		if (string.IsNullOrWhiteSpace(password))
		{
			ShowMessage("Please enter your password.");
			return;
		}

		if (string.IsNullOrWhiteSpace(confirmPassword))
		{
			ShowMessage("Please confirm your password.");
			return;
		}

		if (password != confirmPassword)
		{
			ShowMessage("Passwords do not match.");
			return;
		}

		otpAttempts = 0;
		isRegistrationOTP = true;
		isForgotPasswordOTP = false;
		pendingEmail = email;

		SendProfessorRegistration();
	}

	private void SendProfessorRegistration()
	{
		var registrationData = new Godot.Collections.Dictionary
		{
			{ "firstName", firstNameInput.Text.Trim() },
			{ "lastName", lastNameInput.Text.Trim() },
			{ "email", emailInput.Text.Trim() },
			{ "password", passwordInput.Text },
			{ "confirmPassword", confirmPasswordInput.Text }
		};

		SendPostRequest("/register/professor", registrationData);
	}

	private void OnBackToLoginPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/UserLogin/Professor/ProfessorLogin.tscn");
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/UserManagement/ProfessorManagement.tscn");
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
		isRegistrationOTP = false;
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

		if (isRegistrationOTP)
		{
			var data = new Godot.Collections.Dictionary
			{
				{ "email", pendingEmail },
				{ "otp", otp }
			};

			SendPostRequest("/verify-registration-otp", data);
			return;
		}

		if (isForgotPasswordOTP)
		{
			var data = new Godot.Collections.Dictionary
			{
				{ "email", pendingEmail },
				{ "otp", otp }
			};

			SendPostRequest("/verify-forgot-password-otp", data);
		}
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

		if (isRegistrationOTP)
		{
			var data = new Godot.Collections.Dictionary
			{
				{ "email", pendingEmail }
			};

			SendPostRequest("/resend-registration-otp", data);
			return;
		}

		if (isForgotPasswordOTP)
		{
			var data = new Godot.Collections.Dictionary
			{
				{ "email", pendingEmail }
			};

			SendPostRequest("/resend-forgot-password-otp", data);
		}
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
		Error error = httpRequest.Request(API_BASE_URL + endpoint, headers, HttpClient.Method.Post, json);

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
		if (isRegistrationOTP && !otpPopup.Visible)
		{
			otpInput.Clear();
			OpenOTPPopup();
			SetOTPBusy(false);
			ShowMessage("Registration OTP has been sent to your email.");
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

		if (isRegistrationOTP && otpPopup.Visible)
		{
			otpPopup.Hide();
			otpInput.Clear();
			isRegistrationOTP = false;
			firstNameInput.Clear();
			lastNameInput.Clear();
			emailInput.Clear();
			passwordInput.Clear();
			confirmPasswordInput.Clear();
			ShowMessage("Email verified successfully. Please login.");
			return;
		}

		if (isForgotPasswordOTP && otpPopup.Visible)
		{
			otpPopup.Hide();
			otpInput.Clear();
			SetOTPBusy(false);
			ShowMessage("Password reset successful!");
			return;
		}
	}

	private void ShowMessage(string message)
	{
		if (messageDialog == null)
			return;

		messageDialog.DialogText = message;
		messageDialog.PopupCentered();
	}

	private string GetApiErrorMessage(string responseText)
	{
		if (string.IsNullOrWhiteSpace(responseText))
			return "The server returned an error.";

		if (responseText.Length > 500)
			return "The server returned an unexpected error.";

		return responseText;
	}

	private bool IsPTCEmail(string email)
	{
		return email.EndsWith("@paterostechnologicalcollege.edu.ph", StringComparison.OrdinalIgnoreCase);
	}

	private bool IsDigitsOnly(string value)
	{
		foreach (char c in value)
		{
			if (!char.IsDigit(c))
				return false;
		}

		return true;
	}
}
