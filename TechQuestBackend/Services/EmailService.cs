using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TechQuestBackend.Services
{
    public class EmailService
    {
        private const string SenderEmail =
            "yhajjmagusib@gmail.com";

        private const string AppPassword =
            "frfn jxoy ryrg ucmt";

        public async Task<bool> SendOTP(
            string email,
            string otp)
        {
            try
            {
                var message = new MimeMessage();

                message.From.Add(
                    new MailboxAddress(
                        "TechQuest",
                        SenderEmail
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
                    "smtp.gmail.com",
                    587,
                    SecureSocketOptions.StartTls
                );

                await smtp.AuthenticateAsync(
                    SenderEmail,
                    AppPassword
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
    }
}