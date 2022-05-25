using System.Net;
using System.Text;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using Krompaco.SendGridForEpi.Helpers;
using Krompaco.SendGridForEpi.Services;
using SendGrid;

namespace Krompaco.SendGridForEpi.ScheduledJobs;

[ScheduledPlugIn(DisplayName = "Process SendGrid Mail Queue", GUID = "d979943a-28f7-407f-8584-73745f6110af", Description = "Processes queue and marks queue items as complete which in default implementation moves the item to a log archive.")]
public class MailServiceQueueJob : ScheduledJobBase
{
    private readonly IMailService mailService;

    private readonly ISendGridClient sendGridClient;

    public MailServiceQueueJob(IMailService mailService, ISendGridClient sendGridClient)
    {
        this.mailService = mailService;
        this.sendGridClient = sendGridClient;
    }

    public override string Execute()
    {
        var queue = this.mailService.GetQueue();
        var retrieved = queue.Count;
        var sent = 0;
        var failed = 0;

        foreach (var mail in queue)
        {
            try
            {
                var response = AsyncHelper.RunSync(() => this.sendGridClient.SendEmailAsync(mail.Mail));

                var body = AsyncHelper.RunSync(() => response.Body.ReadAsStringAsync());

                var sb = new StringBuilder();
                sb.AppendLine(response.StatusCode.ToString());
                sb.AppendLine(body);
                sb.AppendLine(response.Headers.ToString());

                if (response.StatusCode == HttpStatusCode.Accepted)
                {
                    this.mailService.MarkComplete(mail.MailQueueItemId);
                    sent++;
                }
                else
                {
                    failed++;
                    this.mailService.MarkError(mail.MailQueueItemId, sb.ToString());
                }
            }
            catch (Exception ex)
            {
                failed++;
                this.mailService.MarkError(mail.MailQueueItemId, $"{ex.Message}\r\n{ex.StackTrace}");
            }
        }

        return $"Processed: {retrieved}, posted: {sent}, failing: {failed}";
    }
}
