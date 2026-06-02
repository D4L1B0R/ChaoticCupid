namespace ChaoticCupid.Models
{
    public class Person
    {
        public string Username { get; set; }
        public string City { get; set; }
        public int Age { get; set; }
        public string Phone { get; set; }
        public bool Gender { get; set; } // true = ženski, false = muški

        public string ConnectionId { get; set; }

        public bool IsBusy { get; set; } = false;
        public HashSet<string> BlockedUsers { get; set; } = new();
        public DateTime? BusySince { get; set; }
    }
}
