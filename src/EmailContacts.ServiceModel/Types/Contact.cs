using ServiceStack.DataAnnotations;

namespace EmailContacts.ServiceModel.Types
{
    public class Contact
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
    }
}