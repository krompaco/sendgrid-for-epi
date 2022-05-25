using System.Collections.Concurrent;
using Krompaco.SendGridForEpi.Extensions;
using Krompaco.SendGridForEpi.Models;
using Krompaco.SendGridForEpi.Services;
using Newtonsoft.Json;
using SendGrid.Helpers.Mail;

namespace Krompaco.SendGridForEpi.Tests.Services;

public class DummyMailService : IMailService
{
    private static readonly ConcurrentDictionary<long, MailQueueItem> Queue = new();

    private static readonly ConcurrentDictionary<long, MailQueueItem> Archive = new();

    public void AddToQueue(MailQueueItem item)
    {
        var personalizations = item.Mail?.Personalizations.ToList() ?? new List<Personalization>();

        foreach (var batch in personalizations.SplitToBatches(1000))
        {
            var itemToSave = new MailQueueItem { Date = DateTime.UtcNow };

            if (item.Mail != null)
            {
                itemToSave.Mail = JsonConvert.DeserializeObject<SendGridMessage>(item.Mail.Serialize()) ?? new SendGridMessage();

                if (itemToSave.Mail != null)
                {
                    itemToSave.Mail.Personalizations = batch.ToList();
                }
            }

            itemToSave.MailQueueItemId = Queue.Any() ? Queue.Max(x => x.Value.MailQueueItemId) + 1 : 1;
            itemToSave.LastAttempt = DateTime.UtcNow;
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

        Queue.TryRemove(mailQueueItemId, out _);
    }

    public void MarkError(long mailQueueItemId, string message)
    {
        var item = Queue[mailQueueItemId];
        item.Attempts++;
        item.AttemptMessage = message;
        item.LastAttempt = DateTime.UtcNow;
    }
}
