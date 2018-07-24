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
        private readonly SentimentSummaryService sentimentSummaryService;

        public DiscordListeners(Config config, SentimentService sentimentService, SentimentHistoryService sentimentHistoryService, SentimentSummaryService sentimentSummaryService)
        {
            this.config = config;
            this.sentimentService = sentimentService;
            this.sentimentHistoryService = sentimentHistoryService;
            this.sentimentSummaryService = sentimentSummaryService;
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
                    var userSentiment = this.sentimentSummaryService.CalculateAverageUserSentiment(message.Channel.Id, message.Author.Id);
                    if(userSentiment >= 0.5)
                    {
                        await message.Channel.SendMessageAsync($"{message.Author.Username}, your current sentiment score is {userSentiment.ToString("0.00")}. Good job, budday!");
                    }
                    else if(userSentiment >= 0)
                    {
                        await message.Channel.SendMessageAsync($"{message.Author.Username}, your sentiment score is {userSentiment.ToString("0.00")} - better work on that.");
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"{message.Author.Username}, no score found for you, friend.");
                    }
                    break;
                case "!analysis":

                    break;
                case "!memory":
                    await message.Channel.SendMessageAsync($"There are {sentimentHistoryService.GetMessages(message.Channel.Id)?.Count ?? 0} messages(s) stored for this channel.");
                    break;
                default:
                    var blocked = this.sentimentHistoryService.CheckSpamBlock(message);
                    if(!blocked)
                    {
                        var result = await sentimentService.AnalyzeMessage(new List<SocketMessage>() { message });
                        this.sentimentHistoryService.StoreMessage(result);
                    }
                    return;
            }
        }

    }
}
