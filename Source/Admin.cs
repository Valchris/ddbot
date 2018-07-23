using DDBot.Configuration;
using DDBot.DependencyInjection;
using DDBot.Listeners;
using DDBot.Models;
using Discord;
using Discord.WebSocket;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DDBot
{
    public class Admin
    {
        public static IContainer DI;
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
            app.Run().GetAwaiter().GetResult();
        }



        public async Task Run()
        {
            var listeners = Admin.DI.GetInstance<IDiscordListeners>();

            var discordClient = new DiscordSocketClient();
            discordClient.Log += listeners.Log;
            discordClient.MessageReceived += listeners.MessageReceived;

            await discordClient.LoginAsync(TokenType.Bot, secrets.DiscordBotToken);
            await discordClient.StartAsync();

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
