namespace Krompaco.SendGridForEpi.Models
{
    using System;

    /// <summary>
    /// The model that can be queued
    /// </summary>
    public class MailQueueItem
    {
        /// <summary>
        /// Gets or sets the Sequential ID to keep track of number of items processed. Will always be overwritten in AddToQueue() method.
        /// </summary>
        public long MailQueueItemId { get; set; }

        /// <summary>
        /// Gets or sets the date and time for the mail, normally you would use DateTime.UtcNow.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the SendGrid Mail object. Typically you would set From, TemplateId and use AddPersonalization().
        /// </summary>
        public SendGrid.Helpers.Mail.SendGridMessage Mail { get; set; }

        /// <summary>
        /// Gets or sets the LastAttempt property. Will always be overwritten to DateTime.UtcNow in AddToQueue() method.
        /// </summary>
        public DateTime LastAttempt { get; set; }

        /// <summary>
        /// Gets or sets the number of attempts. GetAddToQueue() is sorted by ascending Attempts.
        /// </summary>
        public int Attempts { get; set; }

        /// <summary>
        /// Gets or sets the error message of the last attempt.
        /// </summary>
        public string AttemptMessage { get; set; }
    }
}
