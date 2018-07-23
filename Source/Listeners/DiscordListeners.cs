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
        private readonly SentimentService sentimentService;
        private readonly SentimentHistoryService sentimentHistoryService;

        public DiscordListeners(SentimentService sentimentService, SentimentHistoryService sentimentHistory)
        {
            this.sentimentService = sentimentService;
        }

        public Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task MessageReceived(SocketMessage message)
        {
            switch(message.Content)
            {
                case "!ping":
                    await message.Channel.SendMessageAsync("Pong!");
                    break;
                case "!sentiment":

                    break;
                case "!analysis":

                    break;
                default:
                    var result = await sentimentService.AnalyzeMessage(new List<SocketMessage>() { message });
                    return;
            }
        }

    }
}
