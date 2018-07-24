using DDBot.Configuration;
using DDBot.Services;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDBot.Listeners
{
    public class DiscordListeners : IDiscordListeners
    {
        private readonly Config config;
        private readonly SentimentService sentimentService;
        private readonly SentimentHistoryService sentimentHistoryService;

        public DiscordListeners(Config config, SentimentService sentimentService, SentimentHistoryService sentimentHistoryService)
        {
            this.config = config;
            this.sentimentService = sentimentService;
            this.sentimentHistoryService = sentimentHistoryService;
        }

        public Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task MessageReceived(SocketMessage message)
        {
            if(message.Author.Id == this.config.BotUserId)
            {
                return;
            }
            switch(message.Content)
            {
                case "!ping":
                    await message.Channel.SendMessageAsync("Pong!");
                    break;
                case "!sentiment":

                    break;
                case "!analysis":

                    break;
                case "!memory":
                    await message.Channel.SendMessageAsync($"There are {sentimentHistoryService.GetMessages(message.Channel.Id)?.Count ?? 0} messages(s) stored for this channel.");
                    break;
                default:
                    var result = await sentimentService.AnalyzeMessage(new List<SocketMessage>() { message });
                    this.sentimentHistoryService.StoreMessage(result);
                    return;
            }
        }

    }
}
