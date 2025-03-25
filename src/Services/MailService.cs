using System.Net.Mail;
using System.Net;
using System.Text.Json;

namespace diplom.Services
{
    public class MailService
    {
        private readonly MailSettings _mailSettings;
        public MailService()
        {
            string jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "mailSettings.json");
            string jsonString = File.ReadAllText(jsonFilePath);
            var mailConfig = JsonSerializer.Deserialize<MailConfig>(jsonString);

            //_mailSettings = mailConfig?.MailSettings ?? throw new InvalidOperationException("Ошибка при чтении параметров почты.");
        }
        public void SendEmail(string body, string topic, string recipientEmail)
        {

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(_mailSettings.FromEmail); 
            mail.To.Add(recipientEmail);
            mail.Subject = topic;
            mail.Body = body;
            mail.IsBodyHtml = true; 


            SmtpClient smtpClient = new SmtpClient("smtp.mail.ru", 25);
            smtpClient.Credentials = new NetworkCredential(_mailSettings.FromEmail, _mailSettings.Password);
            smtpClient.EnableSsl = true;


            smtpClient.Send(mail);
        }
    }
}
