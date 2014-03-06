using ServiceStack;

namespace EmailContacts.ServiceModel
{
    [Route("/emails")]
    public class FindEmails
    {
        public int? Skip { get; set; }
        public int? Take { get; set; }
        public string To { get; set; }
    }
}