using Godot;
using System;
using System.Text;

public partial class StudentLogin : Control
{
	// =========================================================
	// API
	// =========================================================

	private const string API_BASE_URL =
		"http://localhost:5000/api/Auth";


	// =========================================================
	// NODES
	// =========================================================

	private HttpRequest httpRequest;

	private Panel loginPanel;
	private Panel registerPanel;

	// =========================================================
	// LOGIN
	// =========================================================

	private LineEdit loginEmailInput;
	private LineEdit loginPasswordInput;

	private Button loginButton;
	private Button registerButton;
	private Button backButton;
	private Button forgotPasswordLink;


	// =========================================================
	// REGISTER
	// =========================================================

	private LineEdit firstNameInput;
	private LineEdit lastNameInput;
	private LineEdit registerEmailInput;

	private OptionButton yearLevelDropdown;

	private LineEdit registerPasswordInput;
	private LineEdit confirmPasswordInput;

	private Button createAccountButton;
	private Button backToLoginButton;

	private TextureRect verifiedIcon;


	// =========================================================
	// FORGOT PASSWORD
	// =========================================================

	private Window forgotPasswordPopup;

	private LineEdit forgotEmailInput;
	private LineEdit newPasswordInput;
	private LineEdit confirmNewPasswordInput;

	private Button forgotSaveButton;
	private Button forgotCancelButton;


	// =========================================================
	// OTP
	// =========================================================

	private Window otpPopup;

	private LineEdit otpInput;

	private Button otpConfirmButton;
	private Button otpResendButton;
	private Button otpCancelButton;


	// =========================================================
	// MESSAGE
	// =========================================================

	private AcceptDialog messageDialog;


	// =========================================================
	// OTP STATE
	// =========================================================

	private string pendingEmail = "";

	private bool isRegistrationOTP = false;
	private bool isForgotPasswordOTP = false;

	private int otpAttempts = 0;

	private const int MaxOTPAttempts = 3;


	// =========================================================
	// READY
	// =========================================================

	public override void _Ready()
	{
		// =====================================================
		// HTTP REQUEST
		// =====================================================

		httpRequest = GetNode<HttpRequest>("HTTPRequest");


		// =====================================================
		// PANELS
		// =====================================================

		loginPanel =
			GetNode<Panel>("LoginPanel");

		registerPanel =
			GetNode<Panel>("RegisterPanel");


		// =====================================================
		// LOGIN
		// =====================================================

		loginEmailInput =
			GetNode<LineEdit>("LoginPanel/EmailInput");

		loginPasswordInput =
			GetNode<LineEdit>("LoginPanel/PasswordInput");

		loginButton =
			GetNode<Button>("LoginPanel/LoginButton");

		registerButton =
			GetNode<Button>("LoginPanel/RegisterButton");

		backButton =
			GetNode<Button>("LoginPanel/Back");

		forgotPasswordLink =
			GetNode<Button>("LoginPanel/ForgotPasswordLink");


		// =====================================================
		// REGISTER
		// =====================================================

		firstNameInput =
			GetNode<LineEdit>("RegisterPanel/FirstNameInput");

		lastNameInput =
			GetNode<LineEdit>("RegisterPanel/LastNameInput");

		registerEmailInput =
			GetNode<LineEdit>("RegisterPanel/EmailInput");

		yearLevelDropdown =
			GetNode<OptionButton>("RegisterPanel/YearLevelDropdown");

		registerPasswordInput =
			GetNode<LineEdit>("RegisterPanel/PasswordInput");

		confirmPasswordInput =
			GetNode<LineEdit>("RegisterPanel/ConfirmPasswordInput");

		verifiedIcon =
			GetNode<TextureRect>("RegisterPanel/VerifiedIcon");

		createAccountButton =
			GetNode<Button>("RegisterPanel/RegisterButton");

		backToLoginButton =
			GetNode<Button>("RegisterPanel/BackToLoginButton");


		// =====================================================
		// FORGOT PASSWORD
		// =====================================================

		forgotPasswordPopup =
			GetNode<Window>("ForgotPasswordPopup");

		forgotEmailInput =
			GetNode<LineEdit>(
				"ForgotPasswordPopup/EmailInput"
			);

		newPasswordInput =
			GetNode<LineEdit>(
				"ForgotPasswordPopup/NewPasswordInput"
			);

		confirmNewPasswordInput =
			GetNode<LineEdit>(
				"ForgotPasswordPopup/ConfirmPasswordInput"
			);

		forgotSaveButton =
			GetNode<Button>(
				"ForgotPasswordPopup/SaveButton"
			);

		forgotCancelButton =
			GetNode<Button>(
				"ForgotPasswordPopup/CancelButton"
			);


		// =====================================================
		// OTP
		// =====================================================

		otpPopup =
			GetNode<Window>("OTPPopup");

		otpInput =
			GetNode<LineEdit>("OTPPopup/OTPInput");

		otpConfirmButton =
			GetNode<Button>(
				"OTPPopup/ConfirmButton"
			);

		otpResendButton =
			GetNode<Button>(
				"OTPPopup/ResendButton"
			);

		otpCancelButton =
			GetNode<Button>(
				"OTPPopup/CancelButton"
			);


		// =====================================================
		// MESSAGE
		// =====================================================

		messageDialog =
			GetNode<AcceptDialog>("MessageDialog");


		// =====================================================
		// DEFAULT STATE
		// =====================================================

		registerPanel.Visible = false;

		forgotPasswordPopup.Visible = false;

		otpPopup.Visible = false;

		verifiedIcon.Visible = false;


		// =====================================================
		// BUTTON EVENTS
		// =====================================================

		loginButton.Pressed += OnLoginPressed;

		registerButton.Pressed += OnRegisterPressed;

		backButton.Pressed += OnBackPressed;

		forgotPasswordLink.Pressed +=
			OnForgotPasswordPressed;

		createAccountButton.Pressed +=
			OnCreateAccountPressed;

		backToLoginButton.Pressed +=
			OnBackToLoginPressed;


		// Forgot Password

		forgotSaveButton.Pressed +=
			OnForgotPasswordSavePressed;

		forgotCancelButton.Pressed +=
			OnForgotPasswordCancelPressed;


		// OTP

		otpConfirmButton.Pressed +=
			OnOTPConfirmPressed;

		otpResendButton.Pressed +=
			OnOTPResendPressed;

		otpCancelButton.Pressed +=
			OnOTPCancelPressed;


		// =====================================================
		// HTTP EVENT
		// =====================================================

		httpRequest.RequestCompleted +=
			OnRequestCompleted;
	}


	// =========================================================
	// LOGIN
	// =========================================================

	private void OnLoginPressed()
	{
		string email =
			loginEmailInput.Text.Trim();

		string password =
			loginPasswordInput.Text;


		if (string.IsNullOrWhiteSpace(email))
		{
			ShowMessage(
				"Please enter your email."
			);

			return;
		}


		if (!IsPTCEmail(email))
		{
			ShowMessage(
				"Please use your PTC institutional email."
			);

			return;
		}


		if (string.IsNullOrWhiteSpace(password))
		{
			ShowMessage(
				"Please enter your password."
			);

			return;
		}


		SendLoginRequest(
			email,
			password
		);
	}


	// =========================================================
	// LOGIN API
	// =========================================================

	private void SendLoginRequest(
		string email,
		string password)
	{
		var data =
			new Godot.Collections.Dictionary
			{
				{ "email", email },
				{ "password", password }
			};

		SendPostRequest(
			"/login/student",
			data
		);
	}


	// =========================================================
	// OPEN REGISTER
	// =========================================================

	private void OnRegisterPressed()
	{
		loginPanel.Visible = false;

		registerPanel.Visible = true;

		verifiedIcon.Visible = false;
	}


	// =========================================================
	// BACK TO LOGIN
	// =========================================================

	private void OnBackToLoginPressed()
	{
		registerPanel.Visible = false;

		loginPanel.Visible = true;
	}


	// =========================================================
	// BACK TO USER SELECTION
	// =========================================================

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile(
			"res://scene/UserSelection/UserSelection.tscn"
		);
	}


	// =========================================================
	// STUDENT REGISTRATION
	// =========================================================

	private void OnCreateAccountPressed()
	{
		string firstName =
			firstNameInput.Text.Trim();

		string lastName =
			lastNameInput.Text.Trim();

		string email =
			registerEmailInput.Text.Trim();

		string password =
			registerPasswordInput.Text;

		string confirmPassword =
			confirmPasswordInput.Text;


		// First Name

		if (string.IsNullOrWhiteSpace(firstName))
		{
			ShowMessage(
				"Please enter your first name."
			);

			return;
		}


		// Last Name

		if (string.IsNullOrWhiteSpace(lastName))
		{
			ShowMessage(
				"Please enter your last name."
			);

			return;
		}


		// Email

		if (string.IsNullOrWhiteSpace(email))
		{
			ShowMessage(
				"Please enter your email."
			);

			return;
		}


		// PTC Email

		if (!IsPTCEmail(email))
		{
			ShowMessage(
				"Only PTC institutional email is allowed."
			);

			return;
		}


		// Year Level

		if (yearLevelDropdown.Selected < 0)
		{
			ShowMessage(
				"Please select your year level."
			);

			return;
		}


		// Password

		if (string.IsNullOrWhiteSpace(password))
		{
			ShowMessage(
				"Please enter your password."
			);

			return;
		}


		// Confirm Password

		if (string.IsNullOrWhiteSpace(confirmPassword))
		{
			ShowMessage(
				"Please confirm your password."
			);

			return;
		}


		// Password Match

		if (password != confirmPassword)
		{
			ShowMessage(
				"Passwords do not match."
			);

			return;
		}


		// Reset OTP state

		otpAttempts = 0;

		isRegistrationOTP = true;

		isForgotPasswordOTP = false;

		pendingEmail = email;


		// Send registration request

		SendStudentRegistration();
	}


	// =========================================================
	// SEND STUDENT REGISTRATION
	// =========================================================

	private void SendStudentRegistration()
	{
		int yearLevel =
			yearLevelDropdown.Selected + 1;


		var registrationData =
			new Godot.Collections.Dictionary
			{
				{
					"firstName",
					firstNameInput.Text.Trim()
				},

				{
					"lastName",
					lastNameInput.Text.Trim()
				},

				{
					"email",
					registerEmailInput.Text.Trim()
				},

				{
					"yearLevel",
					yearLevel
				},

				{
					"password",
					registerPasswordInput.Text
				},

				{
					"confirmPassword",
					confirmPasswordInput.Text
				}
			};


		SendPostRequest(
			"/register/student",
			registrationData
		);
	}


	// =========================================================
	// FORGOT PASSWORD
	// =========================================================

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


	// =========================================================
	// FORGOT PASSWORD SAVE
	// =========================================================

	private void OnForgotPasswordSavePressed()
	{
		string email =
			forgotEmailInput.Text.Trim();

		string newPassword =
			newPasswordInput.Text;

		string confirmPassword =
			confirmNewPasswordInput.Text;


		// Email

		if (string.IsNullOrWhiteSpace(email))
		{
			ShowMessage(
				"Please enter your email."
			);

			return;
		}


		// PTC Email

		if (!IsPTCEmail(email))
		{
			ShowMessage(
				"Please use your PTC institutional email."
			);

			return;
		}


		// Password

		if (string.IsNullOrWhiteSpace(newPassword))
		{
			ShowMessage(
				"Please enter your new password."
			);

			return;
		}


		// Confirm Password

		if (string.IsNullOrWhiteSpace(confirmPassword))
		{
			ShowMessage(
				"Please confirm your new password."
			);

			return;
		}


		// Match

		if (newPassword != confirmPassword)
		{
			ShowMessage(
				"Passwords do not match."
			);

			return;
		}


		// OTP state

		otpAttempts = 0;

		isRegistrationOTP = false;

		isForgotPasswordOTP = true;

		pendingEmail = email;
		SetForgotPasswordBusy(true);


		// Send Forgot Password OTP

		var data =
			new Godot.Collections.Dictionary
			{
				{
					"email",
					email
				},

				{
					"newPassword",
					newPassword
				},

				{
					"confirmPassword",
					confirmPassword
				}
			};


		SendPostRequest(
			"/forgot-password",
			data
		);
	}


	// =========================================================
	// CANCEL FORGOT PASSWORD
	// =========================================================

	private void OnForgotPasswordCancelPressed()
	{
		forgotPasswordPopup.Hide();
	}


	// =========================================================
	// OTP CONFIRM
	// =========================================================

	private void OnOTPConfirmPressed()
	{
		string otp =
			otpInput.Text.Trim();


		// Empty OTP

		if (string.IsNullOrWhiteSpace(otp))
		{
			ShowMessage(
				"Please enter the OTP."
			);

			return;
		}


		// Must be 6 digits

		if (otp.Length != 6 ||
			!IsDigitsOnly(otp))
		{
			ShowMessage(
				"OTP must contain exactly 6 digits."
			);

			return;
		}


		// Maximum attempts

		if (otpAttempts >= MaxOTPAttempts)
		{
			ShowMessage(
				"Maximum OTP attempts reached. Please resend a new OTP."
			);

			return;
		}


		otpAttempts++;


		// =====================================================
		// REGISTRATION OTP
		// =====================================================

		if (isRegistrationOTP)
		{
			var data =
				new Godot.Collections.Dictionary
				{
					{
						"email",
						pendingEmail
					},

					{
						"otp",
						otp
					}
				};


			SendPostRequest(
				"/verify-registration-otp",
				data
			);

			return;
		}


		// =====================================================
		// FORGOT PASSWORD OTP
		// =====================================================

		if (isForgotPasswordOTP)
		{
			var data =
				new Godot.Collections.Dictionary
				{
					{
						"email",
						pendingEmail
					},

					{
						"otp",
						otp
					}
				};


			SendPostRequest(
				"/verify-forgot-password-otp",
				data
			);
		}
	}


	// =========================================================
	// OTP RESEND
	// =========================================================

	private void OnOTPResendPressed()
	{
		if (string.IsNullOrWhiteSpace(pendingEmail))
		{
			ShowMessage(
				"Email information is missing."
			);

			return;
		}


		otpInput.Clear();

		otpAttempts = 0;


		// =====================================================
		// REGISTRATION RESEND
		// =====================================================

		if (isRegistrationOTP)
		{
			var data =
				new Godot.Collections.Dictionary
				{
					{
						"email",
						pendingEmail
					}
				};


			SendPostRequest(
				"/resend-registration-otp",
				data
			);

			return;
		}


		// =====================================================
		// FORGOT PASSWORD RESEND
		// =====================================================

		if (isForgotPasswordOTP)
		{
			var data =
				new Godot.Collections.Dictionary
				{
					{
						"email",
						pendingEmail
					}
				};


			SendPostRequest(
				"/resend-forgot-password-otp",
				data
			);
		}
	}


	// =========================================================
	// OTP CANCEL
	// =========================================================

	private void OnOTPCancelPressed()
	{
		otpPopup.Hide();

		otpInput.Clear();

		otpAttempts = 0;
	}


	// =========================================================
	// HTTP POST HELPER
	// =========================================================

	private void SendPostRequest(
		string endpoint,
		Godot.Collections.Dictionary data)
	{
		string json =
			Json.Stringify(data);


		GD.Print(
			"================================="
		);

		GD.Print(
			"API REQUEST"
		);

		GD.Print(
			API_BASE_URL + endpoint
		);

		GD.Print(
			json
		);

		GD.Print(
			"================================="
		);


		string[] headers =
		{
			"Content-Type: application/json"
		};


		Error error =
			httpRequest.Request(
				API_BASE_URL + endpoint,
				headers,
				HttpClient.Method.Post,
				json
			);


		if (error != Error.Ok)
		{
			ShowMessage(
				"Unable to connect to TechQuestBackend."
			);

			GD.PrintErr(
				$"HTTPRequest Error: {error}"
			);
		}
	}


	// =========================================================
	// HTTP RESPONSE
	// =========================================================

	private void OnRequestCompleted(
		long result,
		long responseCode,
		string[] headers,
		byte[] body)
	{
		string responseText =
			Encoding.UTF8.GetString(body);


		GD.Print(
			"================================="
		);

		GD.Print(
			$"HTTP STATUS: {responseCode}"
		);

		GD.Print(
			responseText
		);

		GD.Print(
			"================================="
		);


		// =====================================================
		// CONNECTION ERROR
		// =====================================================

		if (result != (long)HttpRequest.Result.Success)
		{
			ShowMessage(
				"Could not connect to TechQuestBackend."
			);

			return;
		}


		// =====================================================
		// SUCCESS
		// =====================================================

		if (responseCode >= 200 &&
			responseCode < 300)
		{
			HandleSuccessfulResponse(
				responseText
			);

			return;
		}


		// =====================================================
		// ERROR
		// =====================================================

		ShowMessage(
			GetApiErrorMessage(responseText)
		);
	}


	// =========================================================
	// SUCCESS RESPONSE
	// =========================================================

	private void HandleSuccessfulResponse(
		string responseText)
	{
		// =====================================================
		// LOGIN
		// =====================================================

		if (responseText.Contains(
			"login",
			StringComparison.OrdinalIgnoreCase))
		{
			StoreSession(responseText);
			ShowMessage(
				"Login successful!"
			);

			NavigateToRoleDashboard();

			return;
		}


		// =====================================================
		// REGISTRATION OTP SENT
		// =====================================================

		if (isRegistrationOTP &&
			!otpPopup.Visible)
		{
			otpInput.Clear();
			OpenOTPPopup();
			SetOTPBusy(false);

			ShowMessage(
				"Registration OTP has been sent to your email."
			);

			return;
		}


		// =====================================================
		// FORGOT PASSWORD OTP SENT
		// =====================================================

		if (isForgotPasswordOTP &&
			!otpPopup.Visible)
		{
			forgotPasswordPopup.Hide();
			otpInput.Clear();
			OpenOTPPopup();
			SetOTPBusy(false);
			SetForgotPasswordBusy(false);

			ShowMessage(
				"Password reset OTP has been sent to your email."
			);

			return;
		}


		// =====================================================
		// REGISTRATION OTP VERIFIED
		// =====================================================

		if (isRegistrationOTP &&
			otpPopup.Visible)
		{
			otpPopup.Hide();

			otpInput.Clear();

			isRegistrationOTP = false;

			loginPanel.Visible = true;
			registerPanel.Visible = false;
			verifiedIcon.Visible = false;

			registerEmailInput.Editable = true;
			firstNameInput.Clear();
			lastNameInput.Clear();
			registerEmailInput.Clear();
			registerPasswordInput.Clear();
			confirmPasswordInput.Clear();
			yearLevelDropdown.Select(0);

			loginEmailInput.Text = pendingEmail;
			loginPasswordInput.Clear();

			ShowMessage(
				"Email verified successfully. Please login."
			);

			return;
		}


		// =====================================================
		// FORGOT PASSWORD OTP VERIFIED
		// =====================================================

		if (isForgotPasswordOTP &&
			otpPopup.Visible)
		{
			otpPopup.Hide();
			otpInput.Clear();
			SetOTPBusy(false);
			ShowMessage(
				"Password reset successful!"
			);

			// Return to login

			loginPanel.Visible = true;

			registerPanel.Visible = false;

			loginEmailInput.Text =
				pendingEmail;

			loginPasswordInput.Clear();

			return;
		}
	}

	private void StoreSession(string responseText)
	{
		var response = Json.ParseString(responseText).AsGodotDictionary();
		int userId = response.ContainsKey("userId") ? (int)response["userId"] : 0;
		string role = response.ContainsKey("role") ? response["role"].ToString() : "student";
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


	// =========================================================
	// API ERROR MESSAGE
	// =========================================================

	private string GetApiErrorMessage(
		string responseText)
	{
		if (string.IsNullOrWhiteSpace(responseText))
		{
			return "The server returned an error.";
		}


		// Avoid showing huge JSON errors directly

		if (responseText.Length > 500)
		{
			return "The server returned an unexpected error.";
		}


		return responseText;
	}


	// =========================================================
	// PTC EMAIL VALIDATION
	// =========================================================

	private bool IsPTCEmail(
		string email)
	{
		return email.EndsWith(
			"@paterostechnologicalcollege.edu.ph",
			StringComparison.OrdinalIgnoreCase
		);
	}


	// =========================================================
	// CHECK DIGITS
	// =========================================================

	private bool IsDigitsOnly(
		string text)
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


	// =========================================================
	// MESSAGE
	// =========================================================

	private void ShowMessage(
		string message)
	{
		messageDialog.DialogText =
			message;
		messageDialog.Visible = true;
		messageDialog.PopupCentered();
		messageDialog.GrabFocus();
	}
}
