using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Linq;

namespace Certera.Core.Mail
{
    public class MailSender : IDisposable
    {
        private MailSenderInfo _info;
        private SmtpClient _client;

        public void Initialize(MailSenderInfo info)
        {
            _info = info;
            _client = new SmtpClient();
        }

        public void Send(string subject, string body, params string[] recipients)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_info.FromName, _info.FromEmail));
            message.To.AddRange(recipients.Select(x => new MailboxAddress(x)));
            message.Subject = subject;
            message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = body
            };

            EnsureConnected();

            _client.Send(message);
        }

        private void EnsureConnected()
        {
            if (!_client.IsConnected)
            {
                _client.Connect(_info.Host, _info.Port, _info.UseSsl);

                if (_info.Username != null || _info.Password != null)
                {
                    _client.Authenticate(_info.Username, _info.Password);
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _client?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
