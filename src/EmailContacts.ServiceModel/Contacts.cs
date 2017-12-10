using System.Collections.Generic;
using EmailContacts.ServiceModel.Types;
using ServiceStack;

namespace EmailContacts.ServiceModel
{
    [Route("/contacts", "GET")]
    public class FindContacts : IReturn<List<Contact>>
    {
        public int? Age { get; set; }
    }

    [Route("/contacts/{Id}", "GET")]
    public class GetContact : IReturn<Contact>
    {
        public int Id { get; set; }
    }

    [Route("/contacts", "POST")]
    public class CreateContact : IReturnVoid
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
    }

    [Route("/contacts/email", "POST")]
    public class EmailContact : IReturn<EmailContactResponse>
    {
        public int ContactId { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }

    public class EmailContactResponse
    {
        public string Email { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/contacts/{Id}/delete")]
    public class DeleteContact
    {
        public int Id { get; set; }
    }

    [Route("/reset")]
    public class Reset {}
}