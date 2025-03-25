namespace diplom.Services
{
    public class MailSettings
    {
        public string FromEmail { get; set; }
        public string Password { get; set; }
    }
    public class MailConfig
    {
        public MailSettings MailSettings { get; set; }
    }
}
