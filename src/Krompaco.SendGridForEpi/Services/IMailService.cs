using Krompaco.SendGridForEpi.Models;

namespace Krompaco.SendGridForEpi.Services;

/// <summary>
/// The service for adding mail items to queue and handling the queue flow
/// </summary>
public interface IMailService
{
    /// <summary>
    /// Adds a new API POST to queue, handles batching of more than 1000 personalizations internally.
    /// </summary>
    /// <param name="item">A MailQueueItem object with the Mail and Date properties set</param>
    void AddToQueue(MailQueueItem item);

    /// <summary>
    /// Gets the items in queue. Typically only called from the scheduled job.
    /// </summary>
    /// <returns>A list of MailQueueItem objects</returns>
    List<MailQueueItem> GetQueue();

    /// <summary>
    /// Removes item from queue and archives the MailQueueItem object. Typically only called from the scheduled job.
    /// </summary>
    /// <param name="mailQueueItemId">A MailQueueItemId</param>
    void MarkComplete(long mailQueueItemId);

    /// <summary>
    /// Updates attempt count and sets the error message on the MailQueueItem object. Typically only called from the scheduled job.
    /// </summary>
    /// <param name="mailQueueItemId">A MailQueueItemId</param>
    /// <param name="message">A diagnostic message on why the posting failed</param>
    void MarkError(long mailQueueItemId, string message);
}
