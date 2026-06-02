using ChaoticCupid.Models;

namespace ChaoticCupid.Storage
{
    public static class UserStore
    {
        public static List<Person> Users = new();

        public static readonly object Lock = new();
    }
}
