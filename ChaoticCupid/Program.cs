using ChaoticCupid.Hubs;
using ChaoticCupid.Services;

namespace ChaoticCupid
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSignalR();
            builder.Services.AddHostedService<CupidService>();

            var app = builder.Build();

            app.MapHub<CupidHub>("/cupid");

            app.Run();
        }
    }
}
