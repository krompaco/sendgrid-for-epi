using Krompaco.SendGridForEpi.Models;
using Krompaco.SendGridForEpi.Services;
using Krompaco.SendGridForEpi.SqlServer.Services;
using SendGrid.Helpers.Mail;

namespace Krompaco.SendGridForEpi.Tests;

public class SqlServerMailServiceTests
{
    private const string SqlServerConnectionString = "Data Source=.;Initial Catalog=krompaco-epi;Connection Timeout=60;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True";

    [Fact]
    public void TestSqlServerConnection()
    {
        var service = new SqlServerMailService(SqlServerConnectionString);
        service.CreateTablesIfNeeded();
    }

    [Fact]
    public void TestSqlServerServiceQueueFlow()
    {
        IMailService service = new SqlServerMailService(SqlServerConnectionString);

        var item = new MailQueueItem
        {
            Mail = GetNewMailObject(),
            Date = DateTime.UtcNow,
        };

        service.AddToQueue(item);

        var list = service.GetQueue();

        Assert.NotEmpty(list);

        Assert.Equal(item.Mail.TemplateId, list.First().Mail?.TemplateId);

        var item2 = new MailQueueItem
        {
            Mail = GetNewMailObject(),
            Date = DateTime.UtcNow,
        };

        service.AddToQueue(item2);

        list = service.GetQueue();

        Assert.True(list.Count > 1);

        service.MarkError(list.Last().MailQueueItemId, "Some error message.");

        list = service.GetQueue();

        var previousCount = list.Count;

        Assert.Contains(list, x => x.AttemptMessage == "Some error message.");

        // Check that batching is working
        var item3 = new MailQueueItem
        {
            Mail = GetNewMailObject(),
            Date = DateTime.UtcNow,
        };

        for (var i = 2; i < 1101; i++)
        {
            item3.Mail.Personalizations.Add(
                    new Personalization()
                    {
                        Tos = new List<EmailAddress> { new EmailAddress($"somedude{i}@krompaco.nu") },
                        Substitutions = new Dictionary<string, string> { { "{name}", $"Some Dude {i}" } },
                    });
        }

        service.AddToQueue(item3);

        list = service.GetQueue();

        Assert.Equal(previousCount + 2, list.Count);
        Assert.Equal(1000, list.Max(x => x.Mail?.Personalizations.Count));
        Assert.Equal(1, list.Count(x => x.Mail?.Personalizations.Count == 100));

        Assert.Equal("somedude1000@krompaco.nu", list.Single(x => x.Mail?.Personalizations.Count == 1000).Mail?.Personalizations.Last().Tos.First().Email);

        Assert.Equal("somedude1001@krompaco.nu", list.Single(x => x.Mail?.Personalizations.Count == 100).Mail?.Personalizations.First().Tos.First().Email);

        foreach (var m in list)
        {
            service.MarkComplete(m.MailQueueItemId);
        }

        list = service.GetQueue();

        Assert.Empty(list);
    }

    private static SendGridMessage GetNewMailObject()
    {
        var mail = new SendGridMessage
        {
            From = new EmailAddress("test@krompaco.nu", "Krompaco Test"),
            TemplateId = "6069c78d-c65b-4701-867f-16cf95ad5138",
            Subject = "Testing Krompaco åäö",
            Personalizations = new List<Personalization>(),
        };

        var emails = new List<string> { "some.dude@krompaco.nu" };

        foreach (var email in emails)
        {
            mail.Personalizations.Add(
                new Personalization()
                {
                    Tos = new List<EmailAddress> { new(email) },
                    Substitutions = new Dictionary<string, string> { { "{name}", "Some Dude" } },
                });
        }

        return mail;
    }
}
