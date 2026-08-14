using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TechQuestBackend.Data;
using TechQuestBackend.Models;
using TechQuestBackend.Models.DTOs;
using TechQuestBackend.Services;

namespace TechQuestBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly TechQuestDbContext _db;
        private readonly OTPService _otpService;
        private readonly EmailService _emailService;
        private readonly IConfiguration _config;

        public AuthController(
            TechQuestDbContext db,
            OTPService otpService,
            EmailService emailService,
            IConfiguration config)
        {
            _db = db;
            _otpService = otpService;
            _emailService = emailService;
            _config = config;
        }

        [HttpPost("login/student")]
        public async Task<IActionResult> LoginStudent(LoginRequest request)
        {
            var result = await AuthenticateUser(request, "student");
            return result;
        }

        [HttpPost("login/professor")]
        public async Task<IActionResult> LoginProfessor(LoginRequest request)
        {
            var result = await AuthenticateUser(request, "professor");
            return result;
        }

        [HttpPost("login/admin")]
        public async Task<IActionResult> LoginAdmin(LoginRequest request)
        {
            var result = await AuthenticateUser(request, "admin");
            return result;
        }

        [HttpPost("register/student")]
        public async Task<IActionResult> RegisterStudent(RegisterStudentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName))
                return BadRequest("First name is required.");

            if (string.IsNullOrWhiteSpace(request.LastName))
                return BadRequest("Last name is required.");

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            if (!IsPTCEmail(request.Email))
                return BadRequest("Only PTC institutional email is allowed.");

            if (request.YearLevel < 1 || request.YearLevel > 4)
                return BadRequest("Please select a valid year level.");

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Password is required.");

            if (request.Password != request.ConfirmPassword)
                return BadRequest("Passwords do not match.");

            string email = NormalizeEmail(request.Email);

            if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email))
                return BadRequest("This email is already registered.");

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var pendingVerification = new EmailVerification
            {
                Email = email,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                YearLevel = request.YearLevel,
                PendingPasswordHash = passwordHash,
                OtpCode = _otpService.GenerateOTP(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false,
                Attempts = 0
            };

            var oldPending = await _db.EmailVerifications
                .Where(v => v.Email.ToLower() == email && !v.IsUsed)
                .ToListAsync();

            foreach (var item in oldPending)
            {
                item.IsUsed = true;
            }

            _db.EmailVerifications.Add(pendingVerification);
            await _db.SaveChangesAsync();

            bool sent = await _emailService.SendOTP(email, pendingVerification.OtpCode);
            if (!sent)
            {
                _db.EmailVerifications.Remove(pendingVerification);
                await _db.SaveChangesAsync();
                return StatusCode(500, "Failed to send OTP.");
            }

            return Ok(new { message = "OTP sent successfully. Please verify your email." });
        }

        [HttpPost("verify-registration-otp")]
        public async Task<IActionResult> VerifyRegistrationOTP(VerifyOTPRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            if (string.IsNullOrWhiteSpace(request.OTP))
                return BadRequest("OTP is required.");

            if (request.OTP.Length != 6 || !request.OTP.All(char.IsDigit))
                return BadRequest("OTP must contain exactly 6 digits.");

            string email = NormalizeEmail(request.Email);

            var verification = await _db.EmailVerifications
                .Where(v => v.Email.ToLower() == email && !v.IsUsed)
                .OrderByDescending(v => v.Id)
                .FirstOrDefaultAsync();

            if (verification == null)
                return BadRequest("No valid OTP was found.");

            if (verification.Attempts >= 3)
            {
                verification.IsUsed = true;
                await _db.SaveChangesAsync();
                return BadRequest("Maximum OTP attempts exceeded. Please request a new OTP.");
            }

            if (DateTime.UtcNow > verification.ExpiresAt)
            {
                verification.IsUsed = true;
                await _db.SaveChangesAsync();
                return BadRequest("OTP has expired. Please request a new OTP.");
            }

            if (verification.OtpCode != request.OTP)
            {
                verification.Attempts++;
                await _db.SaveChangesAsync();
                return BadRequest($"Invalid OTP. Attempt {verification.Attempts} of 3.");
            }

            if (string.IsNullOrWhiteSpace(verification.PendingPasswordHash))
                return BadRequest("Registration data was not found.");

            if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email))
                return BadRequest("This email is already registered.");

            var user = new User
            {
                FirstName = verification.FirstName,
                LastName = verification.LastName,
                YearLevel = verification.YearLevel,
                Email = email,
                PasswordHash = verification.PendingPasswordHash,
                Role = "student",
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            verification.IsUsed = true;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Email verified successfully. Student account created." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            if (!IsPTCEmail(request.Email))
                return BadRequest("Only PTC institutional email is allowed.");

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest("New password is required.");

            if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
                return BadRequest("Please confirm your new password.");

            if (request.NewPassword != request.ConfirmPassword)
                return BadRequest("Passwords do not match.");

            string email = NormalizeEmail(request.Email);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user == null)
                return BadRequest("No account found for this email.");

            var oldResets = await _db.PasswordResets
                .Where(r => r.Email.ToLower() == email && !r.IsUsed)
                .ToListAsync();

            foreach (var oldReset in oldResets)
            {
                oldReset.IsUsed = true;
            }

            string otpCode = _otpService.GenerateOTP();
            string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            var reset = new PasswordReset
            {
                UserId = user.Id,
                Email = email,
                OtpCode = otpCode,
                NewPasswordHash = newPasswordHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false,
                Attempts = 0
            };

            _db.PasswordResets.Add(reset);
            await _db.SaveChangesAsync();

            bool sent = await _emailService.SendOTP(email, otpCode);
            if (!sent)
            {
                _db.PasswordResets.Remove(reset);
                await _db.SaveChangesAsync();
                return StatusCode(500, "Failed to send OTP.");
            }

            return Ok(new { message = "OTP sent successfully. Please verify your email." });
        }

        [HttpPost("verify-forgot-password-otp")]
        public async Task<IActionResult> VerifyForgotPasswordOTP(VerifyOTPRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            if (string.IsNullOrWhiteSpace(request.OTP))
                return BadRequest("OTP is required.");

            if (request.OTP.Length != 6 || !request.OTP.All(char.IsDigit))
                return BadRequest("OTP must contain exactly 6 digits.");

            string email = NormalizeEmail(request.Email);

            var reset = await _db.PasswordResets
                .Where(r => r.Email.ToLower() == email && !r.IsUsed)
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();

            if (reset == null)
                return BadRequest("No valid OTP was found.");

            if (reset.Attempts >= 3)
            {
                reset.IsUsed = true;
                await _db.SaveChangesAsync();
                return BadRequest("Maximum OTP attempts exceeded. Please request a new OTP.");
            }

            if (DateTime.UtcNow > reset.ExpiresAt)
            {
                reset.IsUsed = true;
                await _db.SaveChangesAsync();
                return BadRequest("OTP has expired. Please request a new OTP.");
            }

            if (reset.OtpCode != request.OTP)
            {
                reset.Attempts++;
                await _db.SaveChangesAsync();
                return BadRequest($"Invalid OTP. Attempt {reset.Attempts} of 3.");
            }

            if (string.IsNullOrWhiteSpace(reset.NewPasswordHash))
                return BadRequest("Password reset request was not found.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user == null)
                return BadRequest("No account found for this email.");

            user.PasswordHash = reset.NewPasswordHash;
            reset.IsUsed = true;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Password reset successful." });
        }

        [HttpPost("resend-registration-otp")]
        public async Task<IActionResult> ResendRegistrationOTP(VerifyOTPRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            string email = NormalizeEmail(request.Email);

            var verification = await _db.EmailVerifications
                .Where(v => v.Email.ToLower() == email && !v.IsUsed)
                .OrderByDescending(v => v.Id)
                .FirstOrDefaultAsync();

            if (verification == null)
                return BadRequest("Pending registration was not found.");

            verification.IsUsed = true;
            var newVerification = new EmailVerification
            {
                Email = verification.Email,
                FirstName = verification.FirstName,
                LastName = verification.LastName,
                YearLevel = verification.YearLevel,
                PendingPasswordHash = verification.PendingPasswordHash,
                OtpCode = _otpService.GenerateOTP(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false,
                Attempts = 0
            };

            _db.EmailVerifications.Add(newVerification);
            await _db.SaveChangesAsync();

            bool sent = await _emailService.SendOTP(email, newVerification.OtpCode);
            if (!sent)
            {
                _db.EmailVerifications.Remove(newVerification);
                await _db.SaveChangesAsync();
                return StatusCode(500, "Failed to send OTP.");
            }

            return Ok(new { message = "OTP resent successfully." });
        }

        [HttpPost("resend-forgot-password-otp")]
        public async Task<IActionResult> ResendForgotPasswordOTP(VerifyOTPRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            string email = NormalizeEmail(request.Email);

            var existingReset = await _db.PasswordResets
                .Where(r => r.Email.ToLower() == email && !r.IsUsed)
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();

            if (existingReset == null)
                return BadRequest("No password reset request was found.");

            existingReset.IsUsed = true;

            var newReset = new PasswordReset
            {
                UserId = existingReset.UserId,
                Email = email,
                OtpCode = _otpService.GenerateOTP(),
                NewPasswordHash = existingReset.NewPasswordHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false,
                Attempts = 0
            };

            _db.PasswordResets.Add(newReset);
            await _db.SaveChangesAsync();

            bool sent = await _emailService.SendOTP(email, newReset.OtpCode);
            if (!sent)
            {
                _db.PasswordResets.Remove(newReset);
                await _db.SaveChangesAsync();
                return StatusCode(500, "Failed to send OTP.");
            }

            return Ok(new { message = "OTP resent successfully." });
        }

        private async Task<IActionResult> AuthenticateUser(LoginRequest request, string role)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Password is required.");

            string email = NormalizeEmail(request.Email);

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Role.ToLower() == role);

            if (user == null)
                return BadRequest("Invalid email or password.");

            bool passwordMatches = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!passwordMatches)
                return BadRequest("Invalid email or password.");

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Login successful.",
                token,
                role = user.Role,
                userId = user.Id,
                redirectTo = user.Role == "student" ? "student-dashboard" : user.Role == "professor" ? "professor-dashboard" : "admin-dashboard"
            });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"] ?? "TECHQUEST_SECRET_KEY_CHANGE_THIS_TO_A_LONG_RANDOM_VALUE";
            var jwtIssuer = _config["Jwt:Issuer"] ?? "TechQuestBackend";
            var jwtAudience = _config["Jwt:Audience"] ?? "TechQuestGodot";

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("userId", user.Id.ToString()),
                new Claim("role", user.Role),
                new Claim(ClaimTypes.Name, user.FirstName + " " + user.LastName)
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLower();
        }

        private static bool IsPTCEmail(string email)
        {
            return email.Trim().EndsWith("@paterostechnologicalcollege.edu.ph", StringComparison.OrdinalIgnoreCase);
        }
    }
}