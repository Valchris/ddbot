using DDBot.Configuration;
using DDBot.DependencyInjection;
using DDBot.Listeners;
using DDBot.Models;
using DDBot.Services;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using StructureMap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DDBot
{
    public class Admin
    {
        public static IContainer DI;
        private static DiscordSocketClient discordClient = new DiscordSocketClient();
        private readonly Secrets secrets;
        private readonly Config config;

        public Admin(Secrets secrets, Config config)
        {
            this.secrets = secrets;
            this.config = config;
        }

        static void Main(string[] args)
        {
           
            var container = Container.For<ConsoleRegistry>();
            DI = container;

            var app = container.GetInstance<Admin>();
            container.Inject<DiscordSocketClient>(discordClient);
            app.Run().GetAwaiter().GetResult();
        }



        public async Task Run()
        {
            var listeners = Admin.DI.GetInstance<IDiscordListeners>();

            discordClient.Ready += listeners.Ready;
            discordClient.JoinedGuild += listeners.JoinedGuild;
            discordClient.Log += listeners.Log;
            discordClient.MessageReceived += listeners.MessageReceived;

            await discordClient.LoginAsync(TokenType.Bot, secrets.DiscordBotToken);
            discordClient.StartAsync().GetAwaiter().GetResult();

            do
            {
                Console.WriteLine("Type 'q' to terminate");
                string input = Console.ReadLine();
                switch (input) {
                    case "q":
                        return;
                    default:
                        break;
                }
            } while (true);
        }
        
    }
}
