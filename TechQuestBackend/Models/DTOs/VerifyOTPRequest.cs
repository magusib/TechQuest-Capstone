namespace TechQuestBackend.Models.DTOs
{
    public class VerifyOTPRequest
    {
        public string Email { get; set; } = "";

        public string OTP { get; set; } = "";
    }
}