namespace TechQuestBackend.Models.DTOs
{
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = "";

        public string NewPassword { get; set; } = "";

        public string ConfirmPassword { get; set; } = "";
    }
}
