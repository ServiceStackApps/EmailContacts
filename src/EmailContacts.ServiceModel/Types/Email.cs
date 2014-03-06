using ServiceStack.DataAnnotations;

namespace EmailContacts.ServiceModel.Types
{
    public class Email
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string To { get; set; }
        public string From { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }
}