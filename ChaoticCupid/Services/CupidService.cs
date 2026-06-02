using ChaoticCupid.Hubs;
using ChaoticCupid.Models;
using ChaoticCupid.Storage;
using Microsoft.AspNetCore.SignalR;
using System.Security.Cryptography;

namespace ChaoticCupid.Services
{
    public class CupidService : BackgroundService
    {
        private readonly IHubContext<CupidHub> _hub;

        public CupidService(IHubContext<CupidHub> hub)
        {
            _hub = hub;
        }

        private Person FindBestMatch(Person receiver)
        {
            List<Person> users;

            lock (UserStore.Lock)
            {
                users = UserStore.Users.ToList();
            }

            Person best = null;
            int bestScore = -1;

            foreach (var p in users)
            {
                if (p.Username == receiver.Username) continue;
                if (receiver.BlockedUsers.Contains(p.Username)) continue;

                if (p.BlockedUsers.Contains(receiver.Username)) continue;

                int score = 0;

                if (p.City == receiver.City)
                    score += 30;

                if (Math.Abs(p.Age - receiver.Age) <= 2)
                    score += 20;

                if (p.Gender != receiver.Gender)
                    score += 15;

                // random faktor
                score += RandomNumberGenerator.GetInt32(0, 101);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }

            return best;
        }

        private string GetMessage()
        {
            string[] msgs =
            {
                "Radujem se našem susretu!",
                "Želim da se upoznamo.",
                "Nisam zainteresovan/a za upoznavanje."
            };

            return msgs[RandomNumberGenerator.GetInt32(0, msgs.Length)];
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

                List<Person> users;

                lock (UserStore.Lock)
                {
                    users = UserStore.Users.ToList();
                }

                foreach (var receiver in users)
                {
                    lock (UserStore.Lock)
                    {
                        if (receiver.IsBusy &&
                            receiver.BusySince.HasValue &&
                            DateTime.UtcNow - receiver.BusySince.Value >
                                TimeSpan.FromMinutes(5))
                        {
                            receiver.IsBusy = false;
                            receiver.BusySince = null;
                        }
                    }

                    if (receiver.IsBusy) continue;

                    var best = FindBestMatch(receiver);
                    if (best == null) continue;

                    lock (UserStore.Lock)
                    {
                        receiver.IsBusy = true;
                        receiver.BusySince = DateTime.UtcNow;
                    }

                    var message = GetMessage();
                    string? phone = null;

                    if (!message.Contains("Nisam zainteresovan"))
                    {
                        phone = best.Phone;
                    }

                    await _hub.Clients.Client(receiver.ConnectionId)
                        .SendAsync("ReceiveLetter",
                            best.Username,
                            best.City,
                            best.Age,
                            phone,
                            message);
                }
            }
        }
    }
}