﻿namespace Krompaco.SendGridForEpi.ScheduledJobs
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.Net;
    using System.Text;

    using EPiServer.PlugIn;
    using EPiServer.Scheduler;

    using Services;

    [ScheduledPlugIn(DisplayName = "Process SendGrid mail queue")]
    public class MailServiceQueueJob : ScheduledJobBase
    {
        private readonly IMailService mailService;

        public MailServiceQueueJob(IMailService mailService)
        {
            this.mailService = mailService;
        }

        public override string Execute()
        {
            var s = new Stopwatch();
            s.Start();

            var config = new SendGridForEpi.Configuration();

            dynamic sg = new SendGrid.SendGridAPIClient(config.ApiKey);

            var queue = this.mailService.GetQueue();
            int retrieved = queue.Count;
            int sent = 0;
            int failed = 0;

            foreach (var mail in queue)
            {
                try
                {
                    dynamic response = sg.client.mail.send.post(requestBody: mail.Mail.Get());

                    var sb = new StringBuilder();
                    sb.AppendLine(response.StatusCode.ToString());
                    sb.AppendLine(response.Body.ReadAsStringAsync().Result);
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

            s.Stop();
            var ts = s.Elapsed;
            string processingTime = $"{Math.Floor(ts.TotalMinutes)} minutes {int.Parse(ts.ToString("ss"))} seconds";
            return $"Processed: {retrieved}, posted: {sent}, failing: {failed}, processing time: {processingTime}";
        }
    }
}