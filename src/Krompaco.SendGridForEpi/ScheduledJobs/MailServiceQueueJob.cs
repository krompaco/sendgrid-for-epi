namespace Krompaco.SendGridForEpi.ScheduledJobs
{
    using System;
    using System.Net;
    using System.Text;

    using EPiServer.PlugIn;
    using EPiServer.Scheduler;
    using Krompaco.SendGridForEpi.Helpers;
    using SendGrid;
    using Services;

    [ScheduledPlugIn(DisplayName = "Process SendGrid Mail Queue", GUID = "d979943a-28f7-407f-8584-73745f6110af", Description = "Processes queue and marks queue items as complete which in default implementation moves the item to a log archive.")]
    public class MailServiceQueueJob : ScheduledJobBase
    {
        private readonly IMailService mailService;

        public MailServiceQueueJob(IMailService mailService)
        {
            this.mailService = mailService;
        }

        public override string Execute()
        {
            var config = new SendGridForEpi.Configuration();

            var client = new SendGridClient(config.ApiKey);

            var queue = this.mailService.GetQueue();
            int retrieved = queue.Count;
            int sent = 0;
            int failed = 0;

            foreach (var mail in queue)
            {
                try
                {
                    string json = mail.Mail.Serialize();

                    var response = AsyncHelper.RunSync(() => client.RequestAsync(
                                                                SendGridClient.Method.POST,
                                                                json,
                                                                urlPath: "mail/send"));

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
                    this.mailService.MarkError(mail.MailQueueItemId, string.Format("{0}\r\n{1}", ex.Message, ex.StackTrace));
                }
            }

            return $"Processed: {retrieved}, posted: {sent}, failing: {failed}";
        }
    }
}