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
using System.Threading;
using System.Threading.Tasks;
using Discord.Audio;
using System.Windows.Forms.DataVisualization.Charting;

namespace DDBot.Listeners
{
    public class DiscordListeners : IDiscordListeners
    {
        private const string HelpText =
@"**Available commands**
```
!sentiment - Your personal sentiment score
!analysis - The overall analysis for the channel
!memory - how many scores are stored for this channel
!joinvoice - joins the voice you are currently connected to
```";

        private readonly Config config;
        private readonly DiscordSocketClient discordClient;
        private readonly SentimentService sentimentService;
        private readonly SentimentHistoryService sentimentHistoryService;
        private readonly SentimentSummaryService sentimentSummaryService;
        private readonly ChartService chartService;
        private readonly SemaphoreSlim semaphore;

        public DiscordListeners(Config config, DiscordSocketClient discordClient, SentimentService sentimentService, SentimentHistoryService sentimentHistoryService, SentimentSummaryService sentimentSummaryService, ChartService chartService)
        {
            this.config = config;
            this.discordClient = discordClient;
            this.sentimentService = sentimentService;
            this.sentimentHistoryService = sentimentHistoryService;
            this.sentimentSummaryService = sentimentSummaryService;
            this.chartService = chartService;
            this.semaphore = new SemaphoreSlim(1);
        }

        public Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task MessageReceived(SocketMessage message)
        {
            if (message.Author.Id == this.config.BotUserId)
            {
                return;
            }
            switch (message.Content)
            {
                case "!help":
                    await message.Channel.SendMessageAsync(HelpText);
                    break;
                case "!ping":
                    await message.Channel.SendMessageAsync("Pong!");
                    break;
                case "!sentiment":
                    var userSentiment = this.sentimentSummaryService.CalculateAverageUserSentiment(message.Channel.Id, message.Author.Id);
                    if (userSentiment >= 0.5)
                    {
                        await message.Channel.SendMessageAsync($"{message.Author.Username}, your current sentiment score is {userSentiment.ToString("0.00")}. Good job, budday!");
                    }
                    else if (userSentiment >= 0)
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
                    foreach (var userData in userLevel)
                    {
                        foreach (var d in userData)
                        {
                            acc += d.Value.Score;
                            count += d.Value.Count;
                        }

                        users[userData.Key] = acc / count;
                    }

                    var analysisOutputSet = users.OrderBy(y => y.Value).Select(x => $"{x.Key}: {(x.Value * 100).ToString("0.00")}%");

                    await message.Channel.SendMessageAsync($"**User Scores**\n```fix\n{string.Join("\n", analysisOutputSet)}```");
                    break;
                //for Danielle's testing only... :)
                case "!dtest":

                    var userDailySummary = this.sentimentSummaryService.GenerateChannelAnalysis(message.Channel.Id);
                    var userDailyLevel = userDailySummary.GroupBy(x => x.Key.Split('-')[0]);
                    List<DataPoint> list = new List<DataPoint>();

                    int userCount = 1;

                    foreach (var userData in userDailyLevel)
                    {
                        int messageCount = 0;
                        double scoreAgg = 0;
                        double dailyScore = 0;

                        foreach (var messageData in userData)
                        {
                            scoreAgg += messageData.Value.Score;
                            messageCount += messageData.Value.Count;
                        }

                        dailyScore = (scoreAgg / messageCount)*100;

                        var item = new DataPoint(userCount, dailyScore)
                        {
                            AxisLabel = userData.Key                            
                        };

                        list.Add(item);
                        userCount++;
                    }

                    List<DataPoint> SortedList = list.OrderBy(o => o.YValues[0]).ToList();

                    chartService.GeneratePlot(SortedList);

                    //List<DataPoint> list = new List<DataPoint>();

                    //var item1 = new DataPoint(1, 24.1);
                    //var item2 = new DataPoint(2, 96.3);
                    //var item3 = new DataPoint(3, 70.7);
                    //var item4 = new DataPoint(4, 40.4);

                    //item1.AxisLabel = "User 1";
                    //item2.AxisLabel = "User 2";
                    //item3.AxisLabel = "User 3";
                    //item4.AxisLabel = "User 4";

                    //list.Add(item1);
                    //list.Add(item2);
                    //list.Add(item3);
                    //list.Add(item4);

                    //chartService.GeneratePlot(list);

                    //await message.Channel.SendMessageAsync($"Items: {item1}, {item2}, {item3}, {item4} ...testing of chart method call complete.");
                    await message.Channel.SendMessageAsync("...testing of chart method call complete.");
                    break;
                case "!memory":
                    await message.Channel.SendMessageAsync($"There are {sentimentHistoryService.GetMessages(message.Channel.Id)?.Count ?? 0} messages(s) stored for this channel.");
                    break;
                default:
                    var blocked = this.sentimentHistoryService.CheckSpamBlock(message);
                    if (!blocked)
                    {
                        var result = await sentimentService.AnalyzeMessage(new List<IMessage>() { message });
                        await this.sentimentHistoryService.StoreMessage(result);
                    }
                    return;
            }
        }

        public async Task Ready()
        {
            await semaphore.WaitAsync();
            try
            {
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
                    // TODO: check access first, do not initialize channels with no permissions
                    var messages = channel.GetMessagesAsync(config.OnChannelJoinBootstrapMessageCount, CacheMode.AllowDownload);
                    try
                    {
                        await messages.ForEachAsync(async (messageSet) =>
                        {
                            if (messageSet.Count() > 0)
                            {
                                var sentimentScores = await this.sentimentService.AnalyzeMessage(messageSet.ToList());
                                await this.sentimentHistoryService.StoreMessage(sentimentScores);
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Cannot initialize a channel: {e} ");
                    }

                    // Mark as initialized
                    initialized.Add(new HasInitialized()
                    {
                        channelId = channel.Id,
                        timestamp = DateTime.UtcNow
                    });
                }

                File.WriteAllText(HasInitializedPath, JsonConvert.SerializeObject(initialized));
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task JoinedGuild(SocketGuild guild)
        {
            await this.Ready();
        }

        public async Task UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
        {
            var x = await arg2.VoiceChannel.ConnectAsync();
            // x.StreamCreated += streamCreated

        }

        //private Task streamCreated(ulong arg1, AudioInStream arg2)
        //{
        //    arg2.
        //}
    }
}
