namespace Krompaco.SendGridForEpi.Models
{
    using System;

    public class MailQueueItem
    {
        public long MailQueueItemId { get; set; }

        public DateTime Date { get; set; }

        public SendGrid.Helpers.Mail.Mail Mail { get; set; }

        public DateTime LastAttempt { get; set; }

        public int Attempts { get; set; }

        public string AttemptMessage { get; set; }
    }
}
