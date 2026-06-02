using ChaoticCupid.Models;
using ChaoticCupid.Storage;
using Microsoft.AspNetCore.SignalR;

namespace ChaoticCupid.Hubs
{
    public class CupidHub : Hub
    {
        public async Task InitSinglePerson(string username, string city, int age, string phone, bool gender)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(city) ||
                age <= 0 ||
                string.IsNullOrWhiteSpace(phone))
            {
                await Clients.Caller.SendAsync("Error", "Neispravni podaci.");
                return;
            }

            bool usernameExists;

            lock (UserStore.Lock)
            {
                usernameExists = UserStore.Users.Any(x =>
                    x.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (!usernameExists)
                {
                    var person = new Person
                    {
                        Username = username,
                        City = city,
                        Age = age,
                        Phone = phone,
                        Gender = gender,
                        ConnectionId = Context.ConnectionId
                    };

                    UserStore.Users.Add(person);
                }
            }

            if (usernameExists)
            {
                await Clients.Caller.SendAsync(
                    "Error",
                    "Korisničko ime već postoji.");

                return;
            }

            await Clients.Caller.SendAsync("Info", "Registracija uspešna.");
        }

        public async Task Block(string username)
        {
            Person current;

            lock (UserStore.Lock)
            {
                current = UserStore.Users
                    .FirstOrDefault(x =>
                        x.ConnectionId == Context.ConnectionId);

                if (current == null)
                    return;
            }

            bool exists;

            lock (UserStore.Lock)
            {
                exists = UserStore.Users.Any(x =>
                    x.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            }

            if (!exists)
            {
                await Clients.Caller.SendAsync("Error", "Korisnik ne postoji.");
                return;
            }

            if (username.Equals(current.Username, StringComparison.OrdinalIgnoreCase))
            {
                await Clients.Caller.SendAsync("Error", "Ne možete blokirati sebe.");
                return;
            }

            lock (UserStore.Lock)
            {
                current.BlockedUsers.Add(username);
            }

            await Clients.Caller.SendAsync("Info", $"Korisnik {username} je blokiran.");
        }

        public Task Confirm()
        {

            lock (UserStore.Lock)
            {
                var user = UserStore.Users
                    .FirstOrDefault(x =>
                        x.ConnectionId == Context.ConnectionId);

                if (user != null)
                {
                    user.IsBusy = false;
                    user.BusySince = null;
                }

                return Task.CompletedTask;
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            lock (UserStore.Lock)
            {
                var user = UserStore.Users
                    .FirstOrDefault(x =>
                        x.ConnectionId == Context.ConnectionId);

                if (user != null)
                {
                    UserStore.Users.Remove(user);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}