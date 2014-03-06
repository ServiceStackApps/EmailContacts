using System.Collections.Generic;
using EmailContacts.ServiceModel;
using EmailContacts.ServiceModel.Types;
using ServiceStack;
using ServiceStack.FluentValidation;
using ServiceStack.OrmLite;

namespace EmailContacts.ServiceInterface
{
    public class CotntactsValidator : AbstractValidator<CreateContact>
    {
        public CotntactsValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("A Name is what's needed.");
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Age).GreaterThan(0);
        }
    }

    public class ContactsServices : Service
    {
        public Contact Any(GetContact request)
        {
            return Db.SingleById<Contact>(request.Id);
        }

        public List<Contact> Any(FindContacts request)
        {
            return request.Age != null
                ? Db.Select<Contact>(q => q.Age == request.Age)
                : Db.Select<Contact>();
        }

        public Contact Post(CreateContact request)
        {
            var contact = request.ConvertTo<Contact>();
            Db.Save(contact);
            return contact;
        }

        public void Any(DeleteContact request)
        {
            Db.DeleteById<Contact>(request.Id);
        }
    }
}
