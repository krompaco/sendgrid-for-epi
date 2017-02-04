namespace Krompaco.SendGridForEpi.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Models;
    using SendGrid.Helpers.Mail;
    using SendGridForEpi.Services;
    using SqlServer.Services;

    [TestClass]
    public class SqlServerMailServiceTests
    {
        [TestMethod]
        public void TestSqlServerConnection()
        {
            var service = new SqlServerService();
            service.CreateTablesIfNeeded();
        }

        [TestMethod]
        public void TestSqlServerServiceQueueFlow()
        {
            IMailService service = new SqlServerMailService();

            var item = new MailQueueItem
            {
                Mail = GetNewMailObject(),
                Date = DateTime.Now,
                Attempts = 0
            };

            service.AddToQueue(item);

            var list = service.GetQueue();

            Assert.IsTrue(list.Any());

            Assert.AreEqual(item.Mail.TemplateId, list.First().Mail.TemplateId);

            var item2 = new MailQueueItem
            {
                Mail = GetNewMailObject(),
                Date = DateTime.Now,
                Attempts = 0
            };

            service.AddToQueue(item2);

            list = service.GetQueue();

            Assert.IsTrue(list.Count > 1);

            service.MarkError(list.Last().MailQueueItemId, "Some error message.");

            list = service.GetQueue();

            var previousCount = list.Count;

            Assert.IsTrue(list.Any(x => x.AttemptMessage == "Some error message."));

            // Check that batching is working
            var item3 = new MailQueueItem
            {
                Mail = GetNewMailObject(),
                Date = DateTime.Now,
                Attempts = 0
            };

            for (int i = 0; i < 1099; i++)
            {
                item3.Mail.AddPersonalization(
                        new Personalization()
                        {
                            Tos = new List<Email> { new SendGrid.Helpers.Mail.Email($"somedude{i}@krompaco.nu") },
                            Substitutions = new Dictionary<string, string> { { "{name}", $"Some Dude {i}" } }
                        });
            }

            service.AddToQueue(item3);

            list = service.GetQueue();

            Assert.AreEqual(previousCount + 2, list.Count);
            Assert.AreEqual(1000, list.Max(x => x.Mail.Personalization.Count));
            Assert.AreEqual(1, list.Count(x => x.Mail.Personalization.Count == 100));

            foreach (var m in list)
            {
                service.MarkComplete(m.MailQueueItemId);
            }

            list = service.GetQueue();

            Assert.AreEqual(0, list.Count);
        }

        private static Mail GetNewMailObject()
        {
            var mail = new Mail
            {
                From = new Email("test@krompaco.nu", "Krompaco Test"),
                TemplateId = "6069c78d-c65b-4701-867f-16cf95ad5138",
                Subject = "Testing Krompaco åäö"
            };

            var emails = new List<string> { "some.dude@krompaco.nu" };

            foreach (var email in emails)
            {
                mail.AddPersonalization(
                    new Personalization()
                    {
                        Tos = new List<Email> { new SendGrid.Helpers.Mail.Email(email) },
                        Substitutions = new Dictionary<string, string> { { "{name}", "Some Dude" } }
                    });
            }

            return mail;
        }
    }
}
