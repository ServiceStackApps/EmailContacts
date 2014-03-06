using EmailContacts.ServiceInterface;
using EmailContacts.ServiceModel;
using EmailContacts.ServiceModel.Types;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Testing;

namespace EmailContacts.Tests
{
    [TestFixture]
    public class UnitTests
    {
        private readonly ServiceStackHost appHost;

        public UnitTests()
        {
            appHost = new BasicAppHost(typeof(EmailServices).Assembly)
            {
                ConfigureContainer = container =>
                {
                    container.Register<IDbConnectionFactory>(c =>
                        new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

                    container.RegisterAs<DbEmailer, IEmailer>();

                    using (var db = container.TryResolve<IDbConnectionFactory>().Open())
                    {
                        db.DropAndCreateTable<Contact>();
                        db.DropAndCreateTable<Email>();

                        db.Insert(new Contact { Name = "Test Contact", Email = "test@email.com", Age = 10 });
                    }
                }
            }
            .Init();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_send_Email_to_TestContact()
        {
            using (var db = appHost.TryResolve<IDbConnectionFactory>().Open())
            using (var service = appHost.TryResolve<EmailServices>())
            {
                var contact = db.Single<Contact>(q => q.Email == "test@email.com");

                var response = service.Any(
                    new EmailContact { ContactId = contact.Id, Subject = "Test Subject" });

                Assert.That(response.Email, Is.EqualTo(contact.Email));

                var email = db.Single<Email>(q => q.To == contact.Email);

                Assert.That(email.Subject, Is.EqualTo("Test Subject"));
            }
        }

        [Test]
        public void Does_throw_when_sending_to_invalid_Contact()
        {
            using (var service = appHost.TryResolve<EmailServices>())
            {
                Assert.Throws<HttpError>(() =>
                    service.Any(new EmailContact { ContactId = -1 }));
            }
        }
    }
}