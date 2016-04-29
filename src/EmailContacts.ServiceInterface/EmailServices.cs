using EmailContacts.ServiceModel;
using EmailContacts.ServiceModel.Types;
using ServiceStack;
using ServiceStack.FluentValidation;
using ServiceStack.OrmLite;

namespace EmailContacts.ServiceInterface
{
    public class EmailContactValidator : AbstractValidator<EmailContact>
    {
        public EmailContactValidator()
        {
            RuleFor(x => x.Subject).NotEmpty();
        }
    }

    public class EmailServices : Service
    {
        public IEmailer Emailer { get; set; }

        public EmailContactResponse Any(EmailContact request)
        {
            var contact = Db.SingleById<Contact>(request.ContactId);
            if (contact == null)
                throw HttpError.NotFound("Contact does not exist");

            var msg = new Email { From = "demo@servicestack.net", To = contact.Email }.PopulateWith(request);
            Emailer.Email(msg);

            return new EmailContactResponse { Email = contact.Email };
        }

        public object Any(FindEmails request)
        {
            var query = Db.From<Email>()
                .OrderByDescending(q => q.Id)
                .Limit(request.Skip.GetValueOrDefault(0), request.Take.GetValueOrDefault(10));

            if (request.To != null)
                query.Where(q => q.To == request.To);

            return Db.Select(query);
        }
    }
}