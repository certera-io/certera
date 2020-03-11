using System.ComponentModel;

namespace Certera.Data.Models
{
    public class NotificationSetting
    {
        public long NotificationSettingId { get; set; }

        [DefaultValue(true)]
        [DisplayName("Expiration Alerts")]
        public bool ExpirationAlerts { get; set; } = true;

        [DefaultValue(true)]
        [DisplayName("Change Alerts")]
        public bool ChangeAlerts { get; set; } = true;

        [DefaultValue(true)]
        [DisplayName("Acquisition Failure Alerts")]
        public bool AcquisitionFailureAlerts { get; set; } = true;

        [DefaultValue(true)]
        [DisplayName("1 day before expiration")]
        public bool ExpirationAlert1Day { get; set; } = true;

        [DefaultValue(true)]
        [DisplayName("3 days before expiration")]
        public bool ExpirationAlert3Days { get; set; } = true;

        [DefaultValue(true)]
        [DisplayName("7 days before expiration")]
        public bool ExpirationAlert7Days { get; set; } = true;

        [DefaultValue(true)]
        [DisplayName("14 days before expiration")]
        public bool ExpirationAlert14Days { get; set; } = true;

        [DefaultValue(true)]
        [DisplayName("30 days before expiration")]
        public bool ExpirationAlert30Days { get; set; } = true;

        [DisplayName("Additional Recipients")]
        public string AdditionalRecipients { get; set; }

        [DefaultValue(true)]
        [DisplayName("Send email notification")]
        public bool SendEmailNotification { get; set; } = true;

        [DefaultValue(false)]
        [DisplayName("Send Slack notification")]
        public bool SendSlackNotification { get; set; }

        [DisplayName("Slack Webhook URL")]
        public string SlackWebhookUrl { get; set; }

        public long ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
    }
}
