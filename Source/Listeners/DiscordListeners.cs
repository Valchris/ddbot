using DDBot.Configuration;
using DDBot.Models;
using DDBot.Services;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDBot.Listeners
{
    public class DiscordListeners : IDiscordListeners
    {
        private readonly Config config;
        private readonly DiscordSocketClient discordClient;
        private readonly SentimentService sentimentService;
        private readonly SentimentHistoryService sentimentHistoryService;
        private readonly SentimentSummaryService sentimentSummaryService;
        private bool InitializedStarted;

        public DiscordListeners(Config config, DiscordSocketClient discordClient, SentimentService sentimentService, SentimentHistoryService sentimentHistoryService, SentimentSummaryService sentimentSummaryService)
        {
            this.InitializedStarted = false;
            this.config = config;
            this.discordClient = discordClient;
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
                    var userDaySummary = this.sentimentSummaryService.GenerateChannelAnalysis(message.Channel.Id);
                    var userLevel = userDaySummary.GroupBy(x => x.Key.Split('-')[0]);
                    Dictionary<string, double> users = new Dictionary<string, double>();

                    double acc = 0;
                    int count = 0;
                    foreach(var userData in userLevel)
                    {
                        foreach(var d in userData)
                        {
                            acc += d.Value.Score;
                            count += d.Value.Count;
                        }

                        users[userData.Key] = acc / count;
                    }

                    var analysisOutputSet = users.OrderBy(y => y.Value).Select(x => $"{x.Key}: {(x.Value * 100).ToString("0.00")}%");
                    
                    await message.Channel.SendMessageAsync($"__User Scores__ \n{string.Join("\n", analysisOutputSet)}");
                    break;
                case "!memory":
                    await message.Channel.SendMessageAsync($"There are {sentimentHistoryService.GetMessages(message.Channel.Id)?.Count ?? 0} messages(s) stored for this channel.");
                    break;
                default:
                    var blocked = this.sentimentHistoryService.CheckSpamBlock(message);
                    if(!blocked)
                    {
                        var result = await sentimentService.AnalyzeMessage(new List<IMessage>() { message });
                        await this.sentimentHistoryService.StoreMessage(result);
                    }
                    return;
            }
        }

        public async Task Ready()
        {
            // Quick abort to prevent multiple runs
            if (this.InitializedStarted) return;

            // Mark this threas as active (weaker than semaphore locking)
            this.InitializedStarted = true;
            var HasInitializedPath = "Data/HasInitialized.json";
            var initialized = new List<HasInitialized>();
            var guilds = this.discordClient.Guilds;
            var channels = guilds.SelectMany(x => x.Channels).Where(z => z as IMessageChannel != null).Select(y => y as IMessageChannel);
            
            // Remove anything already initialized
            if (File.Exists(HasInitializedPath))
            {
                initialized = JsonConvert.DeserializeObject<List<HasInitialized>>(File.ReadAllText(HasInitializedPath));
                channels = channels.Where(x => !initialized.Where(y => y.channelId == x.Id).Any());
            }

            // Iterate all uninitialized channels and bootstrap messages
            foreach (var channel in channels)
            {
                var messages = channel.GetMessagesAsync(250, CacheMode.AllowDownload);
                await messages.ForEachAsync(async (messageSet) =>
                {
                    if(messageSet.Count() > 0)
                    {
                        var sentimentScores = await this.sentimentService.AnalyzeMessage(messageSet.ToList());
                        await this.sentimentHistoryService.StoreMessage(sentimentScores);
                    }
                });

                // Mark as initialized
                initialized.Add(new HasInitialized()
                {
                    channelId = channel.Id,
                    timestamp = DateTime.UtcNow
                });
            }

            File.WriteAllText(HasInitializedPath, JsonConvert.SerializeObject(initialized));
        }
    }
}
