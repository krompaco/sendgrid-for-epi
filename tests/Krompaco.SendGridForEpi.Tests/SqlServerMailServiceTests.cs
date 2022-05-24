using System;
using System.Collections.Generic;
using System.Linq;
using Models;
using SendGrid.Helpers.Mail;
using SendGridForEpi.Services;
using SqlServer.Services;

namespace Krompaco.SendGridForEpi.Tests;

public class SqlServerMailServiceTests
{
    [Fact]
    public void TestSqlServerConnection()
    {
        var service = new SqlServerService();
        service.CreateTablesIfNeeded();
    }

    [Fact]
    public void TestSqlServerServiceQueueFlow()
    {
        IMailService service = new SqlServerMailService();

        var item = new MailQueueItem
        {
            Mail = GetNewMailObject(),
            Date = DateTime.UtcNow
        };

        service.AddToQueue(item);

        var list = service.GetQueue();

        Assert.IsTrue(list.Any());

        Assert.AreEqual(item.Mail.TemplateId, list.First().Mail.TemplateId);

        var item2 = new MailQueueItem
        {
            Mail = GetNewMailObject(),
            Date = DateTime.UtcNow
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
            Date = DateTime.UtcNow
        };

        for (int i = 2; i < 1101; i++)
        {
            item3.Mail.Personalizations.Add(
                    new Personalization()
                    {
                        Tos = new List<EmailAddress> { new EmailAddress($"somedude{i}@krompaco.nu") },
                        Substitutions = new Dictionary<string, string> { { "{name}", $"Some Dude {i}" } }
                    });
        }

        service.AddToQueue(item3);

        list = service.GetQueue();

        Assert.AreEqual(previousCount + 2, list.Count);
        Assert.AreEqual(1000, list.Max(x => x.Mail.Personalizations.Count));
        Assert.AreEqual(1, list.Count(x => x.Mail.Personalizations.Count == 100));

        Assert.AreEqual("somedude1000@krompaco.nu", list.Single(x => x.Mail.Personalizations.Count == 1000).Mail.Personalizations.Last().Tos.First().Email);

        Assert.AreEqual("somedude1001@krompaco.nu", list.Single(x => x.Mail.Personalizations.Count == 100).Mail.Personalizations.First().Tos.First().Email);

        foreach (var m in list)
        {
            service.MarkComplete(m.MailQueueItemId);
        }

        list = service.GetQueue();

        Assert.AreEqual(0, list.Count);
    }

    private static SendGridMessage GetNewMailObject()
    {
        var mail = new SendGridMessage
        {
            From = new EmailAddress("test@krompaco.nu", "Krompaco Test"),
            TemplateId = "6069c78d-c65b-4701-867f-16cf95ad5138",
            Subject = "Testing Krompaco åäö",
            Personalizations = new List<Personalization>()
        };

        var emails = new List<string> { "some.dude@krompaco.nu" };

        foreach (var email in emails)
        {
            mail.Personalizations.Add(
                new Personalization()
                {
                    Tos = new List<EmailAddress> { new EmailAddress(email) },
                    Substitutions = new Dictionary<string, string> { { "{name}", "Some Dude" } }
                });
        }

        return mail;
    }
}
