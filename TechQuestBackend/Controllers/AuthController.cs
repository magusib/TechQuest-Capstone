using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public AuthController(
            TechQuestDbContext db,
            OTPService otpService,
            EmailService emailService)
        {
            _db = db;
            _otpService = otpService;
            _emailService = emailService;
        }

        // =========================================================
        // STUDENT LOGIN
        // POST: /api/Auth/login/student
        // =========================================================

        [HttpPost("login/student")]
        public async Task<IActionResult> LoginStudent(
            LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Email is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Password is required.");
            }

            string email = request.Email.Trim().ToLower();

            var student = await _db.Students
                .FirstOrDefaultAsync(s =>
                    s.Email.ToLower() == email);

            if (student == null)
            {
                return BadRequest("Invalid email or password.");
            }

            bool passwordMatches = BCrypt.Net.BCrypt.Verify(
                request.Password,
                student.PasswordHash);

            if (!passwordMatches)
            {
                return BadRequest("Invalid email or password.");
            }

            return Ok(new
            {
                message = "Login successful."
            });
        }

        // =========================================================
        // PROFESSOR LOGIN
        // POST: /api/Auth/login/professor
        // =========================================================

        [HttpPost("login/professor")]
        public async Task<IActionResult> LoginProfessor(
            LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Email is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Password is required.");
            }

            string email = request.Email.Trim().ToLower();

            var professor = await _db.Professors
                .FirstOrDefaultAsync(p =>
                    p.Email.ToLower() == email);

            if (professor == null)
            {
                return BadRequest("Invalid email or password.");
            }

            bool passwordMatches = BCrypt.Net.BCrypt.Verify(
                request.Password,
                professor.PasswordHash);

            if (!passwordMatches)
            {
                return BadRequest("Invalid email or password.");
            }

            return Ok(new
            {
                message = "Login successful."
            });
        }

        // =========================================================
        // ADMIN LOGIN
        // POST: /api/Auth/login/admin
        // =========================================================

        [HttpPost("login/admin")]
        public async Task<IActionResult> LoginAdmin(
            LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Email is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Password is required.");
            }

            string email = request.Email.Trim().ToLower();

            var admin = await _db.Admins
                .FirstOrDefaultAsync(a =>
                    a.Email.ToLower() == email);

            if (admin == null)
            {
                return BadRequest("Invalid email or password.");
            }

            bool passwordMatches = BCrypt.Net.BCrypt.Verify(
                request.Password,
                admin.PasswordHash);

            if (!passwordMatches)
            {
                return BadRequest("Invalid email or password.");
            }

            return Ok(new
            {
                message = "Login successful."
            });
        }

        // =========================================================
        // STUDENT REGISTRATION
        // POST: /api/Auth/register/student
        // =========================================================

        [HttpPost("register/student")]
        public async Task<IActionResult> RegisterStudent(
            RegisterStudentRequest request)
        {
            // =========================
            // Required Fields
            // =========================

            if (string.IsNullOrWhiteSpace(request.FirstName))
            {
                return BadRequest("First name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.LastName))
            {
                return BadRequest("Last name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Email is required.");
            }

            // =========================
            // PTC Email Validation
            // =========================

            if (!request.Email.EndsWith(
                    "@paterostechnologicalcollege.edu.ph",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "Only PTC institutional email is allowed.");
            }

            // =========================
            // Year Level Validation
            // =========================

            if (request.YearLevel < 1 ||
                request.YearLevel > 4)
            {
                return BadRequest(
                    "Please select a valid year level.");
            }

            // =========================
            // Password Validation
            // =========================

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Password is required.");
            }

            if (request.Password != request.ConfirmPassword)
            {
                return BadRequest("Passwords do not match.");
            }

            // =========================
            // Check Existing Student
            // =========================

            bool emailExists = await _db.Students
                .AnyAsync(s =>
                    s.Email.ToLower() ==
                    request.Email.ToLower());

            if (emailExists)
            {
                return BadRequest(
                    "This email is already registered.");
            }

            // =========================
            // Hash Password
            // =========================

            string passwordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    request.Password);

            // =========================
            // Remove Previous Pending Registration
            // =========================

            var oldPending = await _db.PendingStudents
                .Where(p =>
                    p.Email.ToLower() ==
                    request.Email.ToLower())
                .ToListAsync();

            foreach (var pending in oldPending)
            {
                _db.PendingStudents.Remove(pending);
            }

            // =========================
            // Create Pending Student
            // =========================

            var pendingStudent = new PendingStudent
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = request.Email.Trim().ToLower(),
                YearLevel = request.YearLevel,
                PasswordHash = passwordHash,

                // Registration expires after 5 minutes
                Expiration = DateTime.UtcNow.AddMinutes(5),

                Verified = false
            };

            _db.PendingStudents.Add(pendingStudent);

            // =========================
            // Generate OTP
            // =========================

            string otpCode =
                _otpService.GenerateOTP();

            // =========================
            // Invalidate Previous OTPs
            // =========================

            var oldOtps = await _db.OTPs
                .Where(o =>
                    o.Email.ToLower() ==
                    request.Email.ToLower() &&
                    !o.Used)
                .ToListAsync();

            foreach (var oldOtp in oldOtps)
            {
                oldOtp.Used = true;
            }

            // =========================
            // Create New OTP
            // =========================

            var otp = new OTP
            {
                Email = request.Email.Trim().ToLower(),
                Code = otpCode,

                // OTP expires after 5 minutes
                Expiration = DateTime.UtcNow.AddMinutes(5),

                Used = false,
                Attempts = 0
            };

            _db.OTPs.Add(otp);

            // =========================
            // Save Pending Registration + OTP
            // =========================

            await _db.SaveChangesAsync();

            // =========================
            // Send OTP
            // =========================

            bool sent = await _emailService.SendOTP(
                request.Email,
                otpCode);

            if (!sent)
            {
                return StatusCode(
                    500,
                    "Failed to send OTP.");
            }

            // =========================
            // Success
            // =========================

            return Ok(new
            {
                message =
                    "OTP sent successfully. Please verify your email."
            });
        }

        // =========================================================
        // VERIFY STUDENT REGISTRATION OTP
        // POST: /api/Auth/verify-registration-otp
        // =========================================================

        [HttpPost("verify-registration-otp")]
        public async Task<IActionResult> VerifyRegistrationOTP(
            VerifyOTPRequest request)
        {
            // =========================
            // Validate Email
            // =========================

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Email is required.");
            }

            // =========================
            // Validate OTP
            // =========================

            if (string.IsNullOrWhiteSpace(request.OTP))
            {
                return BadRequest("OTP is required.");
            }

            if (request.OTP.Length != 6 ||
                !request.OTP.All(char.IsDigit))
            {
                return BadRequest(
                    "OTP must contain exactly 6 digits.");
            }

            string email =
                request.Email.Trim().ToLower();

            // =========================
            // Find Latest Valid OTP
            // =========================

            var otp = await _db.OTPs
                .Where(o =>
                    o.Email.ToLower() == email &&
                    !o.Used)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            if (otp == null)
            {
                return BadRequest(
                    "No valid OTP was found.");
            }

            // =========================
            // Check Maximum Attempts
            // =========================

            if (otp.Attempts >= 3)
            {
                otp.Used = true;

                await _db.SaveChangesAsync();

                return BadRequest(
                    "Maximum OTP attempts exceeded. Please request a new OTP.");
            }

            // =========================
            // Check OTP Expiration
            // =========================

            if (DateTime.UtcNow > otp.Expiration)
            {
                otp.Used = true;

                await _db.SaveChangesAsync();

                return BadRequest(
                    "OTP has expired. Please request a new OTP.");
            }

            // =========================
            // Check OTP Code
            // =========================

            if (otp.Code != request.OTP)
            {
                otp.Attempts++;

                await _db.SaveChangesAsync();

                return BadRequest(
                    $"Invalid OTP. Attempt {otp.Attempts} of 3.");
            }

            // =========================
            // Find Pending Student
            // =========================

            var pendingStudent =
                await _db.PendingStudents
                    .FirstOrDefaultAsync(p =>
                        p.Email.ToLower() == email);

            if (pendingStudent == null)
            {
                return BadRequest(
                    "Pending registration was not found.");
            }

            // =========================
            // Check Pending Registration Expiration
            // =========================

            if (DateTime.UtcNow >
                pendingStudent.Expiration)
            {
                _db.PendingStudents
                    .Remove(pendingStudent);

                otp.Used = true;

                await _db.SaveChangesAsync();

                return BadRequest(
                    "Registration has expired. Please register again.");
            }

            // =========================
            // Create Student Account
            // =========================

            var student = new Student
            {
                FirstName = pendingStudent.FirstName,

                LastName = pendingStudent.LastName,

                Email = pendingStudent.Email,

                YearLevel = pendingStudent.YearLevel,

                PasswordHash =
                    pendingStudent.PasswordHash,

                EmailVerified = true
            };

            _db.Students.Add(student);

            // =========================
            // Mark OTP as Used
            // =========================

            otp.Used = true;

            // =========================
            // Remove Pending Registration
            // =========================

            _db.PendingStudents
                .Remove(pendingStudent);

            // =========================
            // Save Everything
            // =========================

            await _db.SaveChangesAsync();

            // =========================
            // Success
            // =========================

            return Ok(new
            {
                message =
                    "Email verified successfully. Student account created."
            });
        }

        // =========================================================
        // FORGOT PASSWORD
        // POST: /api/Auth/forgot-password
        // =========================================================

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Email is required.");
            }

            if (!request.Email.EndsWith(
                    "@paterostechnologicalcollege.edu.ph",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "Only PTC institutional email is allowed.");
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest("New password is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                return BadRequest("Please confirm your new password.");
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest("Passwords do not match.");
            }

            string email = request.Email.Trim().ToLower();

            bool userExists = await _db.Students.AnyAsync(s =>
                    s.Email.ToLower() == email) ||
                await _db.Professors.AnyAsync(p =>
                    p.Email.ToLower() == email) ||
                await _db.Admins.AnyAsync(a =>
                    a.Email.ToLower() == email);

            if (!userExists)
            {
                return BadRequest("No account found for this email.");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(
                request.NewPassword);

            var oldOtps = await _db.OTPs
                .Where(o =>
                    o.Email.ToLower() == email &&
                    !o.Used)
                .ToListAsync();

            foreach (var oldOtp in oldOtps)
            {
                oldOtp.Used = true;
            }

            string otpCode = _otpService.GenerateOTP();

            var otp = new OTP
            {
                Email = email,
                Code = otpCode,
                Expiration = DateTime.UtcNow.AddMinutes(5),
                Used = false,
                Attempts = 0,
                PendingPasswordHash = passwordHash
            };

            _db.OTPs.Add(otp);

            await _db.SaveChangesAsync();

            bool sent = await _emailService.SendOTP(
                request.Email,
                otpCode);

            if (!sent)
            {
                return StatusCode(
                    500,
                    "Failed to send OTP.");
            }

            return Ok(new
            {
                message =
                    "OTP sent successfully. Please verify your email."
            });
        }

        // =========================================================
        // VERIFY FORGOT PASSWORD OTP
        // POST: /api/Auth/verify-forgot-password-otp
        // =========================================================

        [HttpPost("verify-forgot-password-otp")]
        public async Task<IActionResult> VerifyForgotPasswordOTP(
            VerifyOTPRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Email is required.");
            }

            if (string.IsNullOrWhiteSpace(request.OTP))
            {
                return BadRequest("OTP is required.");
            }

            if (request.OTP.Length != 6 ||
                !request.OTP.All(char.IsDigit))
            {
                return BadRequest(
                    "OTP must contain exactly 6 digits.");
            }

            string email = request.Email.Trim().ToLower();

            var otp = await _db.OTPs
                .Where(o =>
                    o.Email.ToLower() == email &&
                    !o.Used)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            if (otp == null)
            {
                return BadRequest("No valid OTP was found.");
            }

            if (otp.Attempts >= 3)
            {
                otp.Used = true;
                await _db.SaveChangesAsync();
                return BadRequest(
                    "Maximum OTP attempts exceeded. Please request a new OTP.");
            }

            if (DateTime.UtcNow > otp.Expiration)
            {
                otp.Used = true;
                await _db.SaveChangesAsync();
                return BadRequest(
                    "OTP has expired. Please request a new OTP.");
            }

            if (otp.Code != request.OTP)
            {
                otp.Attempts++;
                await _db.SaveChangesAsync();
                return BadRequest(
                    $"Invalid OTP. Attempt {otp.Attempts} of 3.");
            }

            if (string.IsNullOrWhiteSpace(otp.PendingPasswordHash))
            {
                return BadRequest(
                    "Password reset request was not found.");
            }

            var student = await _db.Students
                .FirstOrDefaultAsync(s =>
                    s.Email.ToLower() == email);

            if (student != null)
            {
                student.PasswordHash = otp.PendingPasswordHash;
            }
            else
            {
                var professor = await _db.Professors
                    .FirstOrDefaultAsync(p =>
                        p.Email.ToLower() == email);

                if (professor != null)
                {
                    professor.PasswordHash = otp.PendingPasswordHash;
                }
                else
                {
                    var admin = await _db.Admins
                        .FirstOrDefaultAsync(a =>
                            a.Email.ToLower() == email);

                    if (admin == null)
                    {
                        return BadRequest(
                            "No account found for this email.");
                    }

                    admin.PasswordHash = otp.PendingPasswordHash;
                }
            }

            otp.Used = true;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Password reset successful."
            });
        }

        // =========================================================
        // RESEND REGISTRATION OTP
        // POST: /api/Auth/resend-registration-otp
        // =========================================================

        [HttpPost("resend-registration-otp")]
        public async Task<IActionResult> ResendRegistrationOTP(
            VerifyOTPRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Email is required.");
            }

            string email = request.Email.Trim().ToLower();

            var pendingStudent = await _db.PendingStudents
                .FirstOrDefaultAsync(p =>
                    p.Email.ToLower() == email);

            if (pendingStudent == null)
            {
                return BadRequest(
                    "Pending registration was not found.");
            }

            string otpCode = _otpService.GenerateOTP();

            var oldOtps = await _db.OTPs
                .Where(o =>
                    o.Email.ToLower() == email &&
                    !o.Used)
                .ToListAsync();

            foreach (var oldOtp in oldOtps)
            {
                oldOtp.Used = true;
            }

            var otp = new OTP
            {
                Email = email,
                Code = otpCode,
                Expiration = DateTime.UtcNow.AddMinutes(5),
                Used = false,
                Attempts = 0,
                PendingPasswordHash = ""
            };

            _db.OTPs.Add(otp);
            await _db.SaveChangesAsync();

            bool sent = await _emailService.SendOTP(
                request.Email,
                otpCode);

            if (!sent)
            {
                return StatusCode(
                    500,
                    "Failed to send OTP.");
            }

            return Ok(new
            {
                message = "OTP resent successfully."
            });
        }

        // =========================================================
        // RESEND FORGOT PASSWORD OTP
        // POST: /api/Auth/resend-forgot-password-otp
        // =========================================================

        [HttpPost("resend-forgot-password-otp")]
        public async Task<IActionResult> ResendForgotPasswordOTP(
            VerifyOTPRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Email is required.");
            }

            string email = request.Email.Trim().ToLower();

            bool userExists = await _db.Students.AnyAsync(s =>
                    s.Email.ToLower() == email) ||
                await _db.Professors.AnyAsync(p =>
                    p.Email.ToLower() == email) ||
                await _db.Admins.AnyAsync(a =>
                    a.Email.ToLower() == email);

            if (!userExists)
            {
                return BadRequest("No account found for this email.");
            }

            string newPassword = "TechQuestTempPassword123!";
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(
                newPassword);

            var oldOtps = await _db.OTPs
                .Where(o =>
                    o.Email.ToLower() == email &&
                    !o.Used)
                .ToListAsync();

            foreach (var oldOtp in oldOtps)
            {
                oldOtp.Used = true;
            }

            string otpCode = _otpService.GenerateOTP();

            var otp = new OTP
            {
                Email = email,
                Code = otpCode,
                Expiration = DateTime.UtcNow.AddMinutes(5),
                Used = false,
                Attempts = 0,
                PendingPasswordHash = passwordHash
            };

            _db.OTPs.Add(otp);
            await _db.SaveChangesAsync();

            bool sent = await _emailService.SendOTP(
                request.Email,
                otpCode);

            if (!sent)
            {
                return StatusCode(
                    500,
                    "Failed to send OTP.");
            }

            return Ok(new
            {
                message = "OTP resent successfully."
            });
        }
    }
}