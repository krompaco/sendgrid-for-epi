using Krompaco.SendGridForEpi.Models;
using Krompaco.SendGridForEpi.Services;
using Krompaco.SendGridForEpi.Tests.Services;
using Newtonsoft.Json;
using SendGrid.Helpers.Mail;
using Xunit.Abstractions;

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

    private readonly ITestOutputHelper testOutputHelper;

    public DummyMailServiceTests(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void SerializesAsExpected()
    {
        var mail = GetNewMailObject();

        var body = mail.Serialize();

        this.testOutputHelper.WriteLine(body);

        Assert.Equal(JsonString, body);
    }

    [Fact]
    public void DeserializesAsExpected()
    {
        var mail = JsonConvert.DeserializeObject<SendGridMessage>(JsonString) ?? new SendGridMessage();
        var deserializedAndBackUsingNormalWay = mail.Serialize();

        this.testOutputHelper.WriteLine(deserializedAndBackUsingNormalWay);

        Assert.Equal(GetNewMailObject().Serialize(), deserializedAndBackUsingNormalWay);
    }

    [Fact]
    public void DeserializesAsExpectedWithFormattedString()
    {
        var original = GetNewMailObject();
        var mail = JsonConvert.DeserializeObject<SendGridMessage>(FormattedJsonString);
        var deserializedAndBackUsingNormalWay = mail?.Serialize();

        this.testOutputHelper.WriteLine(deserializedAndBackUsingNormalWay);

        Assert.Equal(original.Serialize(), deserializedAndBackUsingNormalWay);
    }

    [Fact]
    public void TestDummyMailServiceQueueFlow()
    {
        IMailService service = new DummyMailService();

        var item = new MailQueueItem
        {
            Mail = GetNewMailObject(),
            Date = DateTime.UtcNow,
        };

        service.AddToQueue(item);

        var list = service.GetQueue();

        Assert.Single(list);

        Assert.Equal(item.Mail.TemplateId, list.First().Mail?.TemplateId);

        var item2 = new MailQueueItem
        {
            Mail = GetNewMailObject(),
            Date = DateTime.UtcNow,
        };

        service.AddToQueue(item2);

        list = service.GetQueue();

        Assert.Equal(2, list.Count);
        Assert.Equal(item2.Mail.TemplateId, list.Last().Mail?.TemplateId);

        service.MarkComplete(list.First().MailQueueItemId);

        service.MarkError(list.Last().MailQueueItemId, "Some error message.");

        list = service.GetQueue();

        Assert.Single(list);
        Assert.Equal(2, list.First().MailQueueItemId);
        Assert.Equal("Some error message.", list.First().AttemptMessage);

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

        Assert.Equal(3, list.Count);
        Assert.Equal(1000, list.Max(x => x.Mail?.Personalizations.Count));
        Assert.Equal(1, list.Count(x => x.Mail?.Personalizations.Count == 100));

        Assert.Equal("somedude1000@krompaco.nu", list.Single(x => x.Mail?.Personalizations.Count == 1000).Mail?.Personalizations.Last().Tos.First().Email);

        Assert.Equal("somedude1001@krompaco.nu", list.Single(x => x.Mail?.Personalizations.Count == 100).Mail?.Personalizations.First().Tos.First().Email);
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
                    Tos = new List<EmailAddress> { new EmailAddress(email) },
                    Substitutions = new Dictionary<string, string> { { "{name}", "Some Dude" } },
                });
        }

        return mail;
    }
}
