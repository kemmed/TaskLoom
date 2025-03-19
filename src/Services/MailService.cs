using System.Net.Mail;
using System.Net;

namespace diplom.Services
{
    public class MailService
    {
        public void SendEmail(string body, string topic, string recipientEmail)
        {

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("task.loom@mail.ru");
            mail.To.Add(recipientEmail);
            mail.Subject = topic;
            mail.Body = body;
            mail.IsBodyHtml = true; 


            SmtpClient smtpClient = new SmtpClient("smtp.mail.ru", 25);
            smtpClient.Credentials = new NetworkCredential("task.loom@mail.ru", "hd4r9uPHfBAyqkUc0wrJ");
            smtpClient.EnableSsl = true;


            smtpClient.Send(mail);
        }
    }
}
