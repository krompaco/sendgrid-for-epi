namespace Krompaco.SendGridForEpi.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Models;
    using Newtonsoft.Json;
    using SendGrid.Helpers.Mail;
    using SendGridForEpi.Services;
    using Services;

    [TestClass]
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

        [TestMethod]
        public void SerializesAsExpected()
        {
            var mail = GetNewMailObject();

            string body = mail.Get();

            Console.WriteLine(body);

            Assert.AreEqual(JsonString, body);
        }

        [TestMethod]
        public void DeserializesAsExpected()
        {
            Mail mail = JsonConvert.DeserializeObject<Mail>(JsonString);
            string deserializedAndBackUsingNormalWay = mail.Get();

            Console.WriteLine(deserializedAndBackUsingNormalWay);

            Assert.AreEqual(GetNewMailObject().Get(), deserializedAndBackUsingNormalWay);
        }

        [TestMethod]
        public void DeserializesAsExpectedWithFormattedString()
        {
            var original = GetNewMailObject();
            Mail mail = JsonConvert.DeserializeObject<Mail>(FormattedJsonString);
            string deserializedAndBackUsingNormalWay = mail.Get();

            Console.WriteLine(deserializedAndBackUsingNormalWay);

            Assert.AreEqual(original.Get(), deserializedAndBackUsingNormalWay);
        }

        [TestMethod]
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

            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(1000, list.Max(x => x.Mail.Personalization.Count));
            Assert.AreEqual(1, list.Count(x => x.Mail.Personalization.Count == 100));
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
