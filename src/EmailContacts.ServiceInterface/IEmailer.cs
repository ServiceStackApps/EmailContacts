using System.Net;
using System.Net.Mail;
using System.Threading;
using EmailContacts.ServiceModel.Types;
using ServiceStack;
using ServiceStack.OrmLite;

namespace EmailContacts.ServiceInterface
{
    public interface IEmailer
    {
        void Email(Email email);
    }

    public class SmtpEmailer : RepositoryBase, IEmailer
    {
        public SmtpConfig Config { get; set; }

        public void Email(Email email)
        {
            var msg = new MailMessage(email.From, email.To).PopulateWith(email);
            using (var client = new SmtpClient(Config.Host, Config.Port))
            {
                client.Credentials = new NetworkCredential(Config.UserName, Config.Password);
                client.EnableSsl = true;
                client.Send(msg);
            }

            Db.Save(email);
        }
    }

    public class DbEmailer : RepositoryBase, IEmailer
    {
        public void Email(Email email)
        {
            Thread.Sleep(1000);  //simulate processing delay
            Db.Save(email);
        }
    }
}