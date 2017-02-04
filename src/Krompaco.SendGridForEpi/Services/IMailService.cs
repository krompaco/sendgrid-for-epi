namespace Krompaco.SendGridForEpi.Services
{
    using System.Collections.Generic;

    using Models;

    public interface IMailService
    {
        void AddToQueue(MailQueueItem item);

        List<MailQueueItem> GetQueue();

        void MarkComplete(long mailQueueItemId);

        void MarkError(long mailQueueItemId, string message);
    }
}
