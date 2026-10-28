using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TechQuestBackend.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendOTP(
            string email,
            string otp)
        {
            try
            {
                string senderEmail = GetRequiredSetting("Email:SenderEmail");
                string appPassword = GetRequiredSetting("Email:AppPassword");
                string smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
                int smtpPort = int.TryParse(_configuration["Email:SmtpPort"], out int configuredPort)
                    ? configuredPort
                    : 587;

                var message = new MimeMessage();

                message.From.Add(
                    new MailboxAddress(
                        "TechQuest",
                        senderEmail
                    )
                );

                message.To.Add(
                    new MailboxAddress(
                        "TechQuest Student",
                        email
                    )
                );

                message.Subject =
                    "TechQuest OTP Verification";

                message.Body = new TextPart("plain")
                {
                    Text =
                        $"Hello,\n\n" +
                        $"Your TechQuest OTP is:\n\n" +
                        $"{otp}\n\n" +
                        $"This OTP expires in 5 minutes.\n\n" +
                        $"If you did not request this OTP, " +
                        $"please ignore this email.\n\n" +
                        $"TechQuest Team"
                };

                using var smtp = new SmtpClient();

                await smtp.ConnectAsync(
                    smtpHost,
                    smtpPort,
                    smtpPort == 465
                        ? SecureSocketOptions.SslOnConnect
                        : SecureSocketOptions.StartTls
                );

                await smtp.AuthenticateAsync(
                    senderEmail,
                    appPassword
                );

                await smtp.SendAsync(message);

                await smtp.DisconnectAsync(true);

                Console.WriteLine(
                    $"OTP email successfully sent to {email}"
                );

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "OTP EMAIL ERROR:"
                );

                Console.WriteLine(ex.Message);

                return false;
            }
        }

        private string GetRequiredSetting(string key)
        {
            return _configuration[key]
                ?? throw new InvalidOperationException($"Missing email configuration: {key}");
        }
    }
}
