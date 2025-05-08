namespace diplom.Services
{
    public class MailSettings
    {
#pragma warning disable CS8618
        public string FromEmail { get; set; }
        public string Password { get; set; }
    }
    public class MailConfig
    {
        public MailSettings MailSettings { get; set; }
    }
}
