using EmailContacts.ServiceModel.Types;
using ServiceStack;

namespace EmailContacts.ServiceModel
{
    [Route("/query/emails")]
    public class QueryEmails : QueryBase<Email> { }

    [Route("/query/contacts")]
    public class QueryContacts : QueryBase<Contact> { }
}