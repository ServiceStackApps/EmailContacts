using System.Collections.Generic;
using System.Threading.Tasks;
using EmailContacts.ServiceModel;
using EmailContacts.ServiceModel.Types;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.RabbitMq;
using ServiceStack.Text;

namespace EmailContacts.Tests
{
    [TestFixture]
    public class IntegrationTests
    {
        readonly IServiceClient client = new JsonServiceClient("http://localhost:5001/");

        [Test]
        public void Can_call_with_JsonServiceClient()
        {
            client.Post(new CreateContact { Name = "Unit Test", Email = "demo+unit@servicestack.net", Age = 27 });

            Contact contact = client.Get(new GetContact { Id = 1 });

            "GetContact: ".Print();
            contact.PrintDump();

            List<Contact> response = client.Get(new FindContacts { Age = 27 });

            "FindContacts: ".Print();
            response.PrintDump();

        }

        [Test]
        public async Task Can_call_with_JsonServiceClient_Async()
        {
            List<Contact> response = await client.GetAsync(new FindContacts { Age = 27 });

            response.PrintDump();
        }

        [Test]
        public void Does_throw_on_invalid_requests()
        {
            try
            {
                client.Post(new EmailContact { ContactId = -1, Subject = "Test" });
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(404));
                Assert.That(ex.ResponseStatus.Message, Is.EqualTo("Contact does not exist"));
            }
        }

        [Test]
        public void Can_Send_Email_via_HttpClient()
        {
            client.Post(new EmailContact { ContactId = 1, Subject = "UnitTest HTTP Email #1", Body = "Body 1" });
            client.Post(new EmailContact { ContactId = 1, Subject = "UnitTest HTTP Email #2", Body = "Body 2" });
        }

        [Test]
        public void Can_Send_Email_via_MqClient()
        {
            var mqFactory = new RabbitMqMessageFactory();

            using (var mqClient = mqFactory.CreateMessageQueueClient())
            {
                mqClient.Publish(new EmailContact { ContactId = 1, Subject = "UnitTest MQ Email #1", Body = "Body 1" });
                mqClient.Publish(new EmailContact { ContactId = 1, Subject = "UnitTest MQ Email #2", Body = "Body 2" });
            }
        }

    }

}
