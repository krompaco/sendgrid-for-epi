using System;
using System.Collections.Generic;
using System.Linq;
using Models;
using Newtonsoft.Json;
using SendGrid.Helpers.Mail;
using SendGridForEpi.Services;
using Services;

namespace Krompaco.SendGridForEpi.Tests;

public class DummyMailServiceTests
{
    private const string JsonString =
        @"{""from"":{""name"":""Krompaco Test"",""email"":""test@krompaco.nu""},""subject"":""Testing Krompaco åäö"",""personalizations"":[{""to"":[{""email"":""some.dude@krompaco.nu""}],""substitutions"":{""{name}"":""Some Dude""}}],""template_id"":""6069c78d-c65b-4701-867f-16cf95ad5138""}";

    private const string FormattedJsonString = @"{
	""from"" : {
		""name"" : ""Krompaco Test"",
		""email"" : ""test@krompaco.nu""
	},
	""subject"" : ""Testing Krompaco åäö"",
	""personalizations"" : [{
			""to"" : [{
					""email"" : ""some.dude@krompaco.nu""
				}
			],
			""substitutions"" : {
				""{name}"" : ""Some Dude""
			}
		}
	],
	""template_id"" : ""6069c78d-c65b-4701-867f-16cf95ad5138""
}
";

    [Fact]
    public void SerializesAsExpected()
    {
        var mail = GetNewMailObject();

        string body = mail.Serialize();

        Console.WriteLine(body);

        Assert.AreEqual(JsonString, body);
    }

    [Fact]
    public void DeserializesAsExpected()
    {
        SendGridMessage mail = JsonConvert.DeserializeObject<SendGridMessage>(JsonString);
        string deserializedAndBackUsingNormalWay = mail.Serialize();

        Console.WriteLine(deserializedAndBackUsingNormalWay);

        Assert.AreEqual(GetNewMailObject().Serialize(), deserializedAndBackUsingNormalWay);
    }

    [Fact]
    public void DeserializesAsExpectedWithFormattedString()
    {
        var original = GetNewMailObject();
        SendGridMessage mail = JsonConvert.DeserializeObject<SendGridMessage>(FormattedJsonString);
        string deserializedAndBackUsingNormalWay = mail.Serialize();

        Console.WriteLine(deserializedAndBackUsingNormalWay);

        Assert.AreEqual(original.Serialize(), deserializedAndBackUsingNormalWay);
    }

    [Fact]
    public void TestDummyMailServiceQueueFlow()
    {
        IMailService service = new DummyMailService();

        var item = new MailQueueItem
        {
            Mail = GetNewMailObject(),
            Date = DateTime.UtcNow
        };

        service.AddToQueue(item);

        var list = service.GetQueue();

        Assert.AreEqual(list.Count, 1);

        Assert.AreEqual(item.Mail.TemplateId, list.First().Mail.TemplateId);

        var item2 = new MailQueueItem
        {
            Mail = GetNewMailObject(),
            Date = DateTime.UtcNow
        };

        service.AddToQueue(item2);

        list = service.GetQueue();

        Assert.AreEqual(2, list.Count);
        Assert.AreEqual(item2.Mail.TemplateId, list.Last().Mail.TemplateId);

        service.MarkComplete(list.First().MailQueueItemId);

        service.MarkError(list.Last().MailQueueItemId, "Some error message.");

        list = service.GetQueue();

        Assert.AreEqual(1, list.Count);
        Assert.AreEqual(2, list.First().MailQueueItemId);
        Assert.AreEqual("Some error message.", list.First().AttemptMessage);

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

        Assert.AreEqual(3, list.Count);
        Assert.AreEqual(1000, list.Max(x => x.Mail.Personalizations.Count));
        Assert.AreEqual(1, list.Count(x => x.Mail.Personalizations.Count == 100));

        Assert.AreEqual("somedude1000@krompaco.nu", list.Single(x => x.Mail.Personalizations.Count == 1000).Mail.Personalizations.Last().Tos.First().Email);

        Assert.AreEqual("somedude1001@krompaco.nu", list.Single(x => x.Mail.Personalizations.Count == 100).Mail.Personalizations.First().Tos.First().Email);
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
