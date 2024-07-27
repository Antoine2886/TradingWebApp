using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace WebApp.Utilities.Email
{
    public class EmailSender : IEmailSender
    {
        /// <summary>
        /// Send an email. YES, this is a real email. THIS IS USING OUTLOOK, use outlook too if you dont want to change anything. thia can a be a regular email
        /// </summary>
        /// <param name="email"></param>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task SendEmailAsync(string email, string subject, string message)
        {
            var mail = Environment.GetEnvironmentVariable("EMAIL_ADDRESS");
            var pw = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");

            if (string.IsNullOrEmpty(mail) || string.IsNullOrEmpty(pw))
            {
                throw new InvalidOperationException("Email credentials are not set in environment variables.");
            }

            var client = new SmtpClient("smtp-mail.outlook.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(mail, pw)
            };

            var mailMessage = new MailMessage(from: mail, to: email, subject, message)
            {
                IsBodyHtml = true
            };

            return client.SendMailAsync(mailMessage);
        }
    }
}