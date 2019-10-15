using System;

namespace Certera.Data.Models
{
    public class UserNotification
    {
        public long UserNotificationId { get; set; }
        public DateTime DateCreated { get; set; }

        public long ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        public long DomainCertificateId { get; set; }
        public DomainCertificate DomainCertificate { get; set; }

        public NotificationEvent NotificationEvent { get; set; }
    }

    public enum NotificationEvent
    {
        ExpirationAlert1Day,
        ExpirationAlert3Days,
        ExpirationAlert7Days,
        ExpirationAlert14Days,
        ExpirationAlert30Days
    }
}
