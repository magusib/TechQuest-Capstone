using System.Security.Cryptography;

namespace TechQuestBackend.Services
{
    public class OTPService
    {
        public string GenerateOTP()
        {
            return RandomNumberGenerator
                .GetInt32(100000, 1000000)
                .ToString();
        }
    }
}