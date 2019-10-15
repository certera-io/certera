namespace Certera.Core.Mail
{
    public class MailSenderInfo
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool UseSsl { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
    }
}
