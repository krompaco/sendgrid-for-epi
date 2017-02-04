namespace Krompaco.SendGridForEpi.Tests.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Extensions;
    using Krompaco.SendGridForEpi.Models;
    using Krompaco.SendGridForEpi.Services;
    using Newtonsoft.Json;
    using SendGrid.Helpers.Mail;

    public class DummyMailService : IMailService
    {
        private static readonly ConcurrentDictionary<long, MailQueueItem> Queue = new ConcurrentDictionary<long, MailQueueItem>();

        private static readonly ConcurrentDictionary<long, MailQueueItem> Archive = new ConcurrentDictionary<long, MailQueueItem>();

        public void AddToQueue(MailQueueItem item)
        {
            var personalizations = item.Mail.Personalization.ToList();

            foreach (var batch in personalizations.SplitToBatches(1000))
            {
                var itemToSave = new MailQueueItem();

                itemToSave.Date = DateTime.Now;
                itemToSave.Mail = JsonConvert.DeserializeObject<Mail>(item.Mail.Get());
                itemToSave.Mail.Personalization = batch.ToList();
                itemToSave.MailQueueItemId = Queue.Any() ? Queue.Max(x => x.Value.MailQueueItemId) + 1 : 1;
                itemToSave.LastAttempt = DateTime.Now;
                itemToSave.Attempts++;

                Queue.TryAdd(itemToSave.MailQueueItemId, itemToSave);
            }
        }

        public List<MailQueueItem> GetQueue()
        {
            return Queue.Select(x => x.Value).ToList();
        }

        public void MarkComplete(long mailQueueItemId)
        {
            var item = Queue[mailQueueItemId];
            Archive.TryAdd(mailQueueItemId, item);

            MailQueueItem x;
            Queue.TryRemove(mailQueueItemId, out x);
        }

        public void MarkError(long mailQueueItemId, string message)
        {
            var item = Queue[mailQueueItemId];
            item.Attempts++;
            item.AttemptMessage = message;
            item.LastAttempt = DateTime.Now;
        }
    }
}
