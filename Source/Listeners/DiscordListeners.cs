using DDBot.Configuration;
using DDBot.Models;
using DDBot.Services;
using Discord;
using Discord.Net;
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
using NAudio.Wave;

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
        private readonly VoiceToTextService voiceToTextService;
        private readonly SemaphoreSlim InitializedSempahore;
        private readonly Dictionary<ulong, SemaphoreSlim> SpeakingSemaphores;
        private readonly Dictionary<ulong, VoiceStream> UserChannels;

        public DiscordListeners(Config config, DiscordSocketClient discordClient, SentimentService sentimentService, SentimentHistoryService sentimentHistoryService, SentimentSummaryService sentimentSummaryService, VoiceToTextService voiceToTextService, ChartService chartService)
        {
            this.config = config;
            this.discordClient = discordClient;
            this.sentimentService = sentimentService;
            this.sentimentHistoryService = sentimentHistoryService;
            this.sentimentSummaryService = sentimentSummaryService;
            this.chartService = chartService;
            this.semaphore = new SemaphoreSlim(1);

            this.voiceToTextService = voiceToTextService;
            this.InitializedSempahore = new SemaphoreSlim(1);
            this.SpeakingSemaphores = new Dictionary<ulong, SemaphoreSlim>();
            this.UserChannels = new Dictionary<ulong, VoiceStream>();
        }

        public Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task MessageReceived(SocketMessage message)
        {
            if(message.Author.IsBot)
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

                        dailyScore = (scoreAgg / messageCount) * 100;

                        var item = new DataPoint(userCount, dailyScore)
                        {
                            AxisLabel = userData.Key
                        };

                        list.Add(item);
                        userCount++;
                    }

                    List<DataPoint> SortedList = list.OrderBy(o => o.YValues[0]).ToList();

                    chartService.GeneratePlot(SortedList);

                    await message.Channel.SendFileAsync("a_mypic.png", $"Channel-wide \"Sentiment\" scores... {SortedList[0].AxisLabel} could use a hug.");
                    break;
                case "!memory":
                    await message.Channel.SendMessageAsync($"There are {sentimentHistoryService.GetMessages(message.Channel.Id)?.Count ?? 0} messages(s) stored for this channel.");
                    break;
                case "!joinvoice":
                    var author = message.Author as SocketGuildUser;
                    if(author?.VoiceChannel?.Id != null)
                    {
                        await Task.Factory.StartNew(async () =>
                        {
                            var audioClient = await author.VoiceChannel.ConnectAsync();
                        });
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"You aren't in a voice channel, please connect first then re-issue command.");
                    }
                    break;
                default:
                    var blocked = this.sentimentHistoryService.CheckSpamBlock(message);
                    if (!blocked)
                    {
                        var result = await sentimentService.AnalyzeMessage(new List<SentimentMessage>() { new SentimentMessage(message) });
                        await this.sentimentHistoryService.StoreMessage(result);
                    }
                    return;
            }
        }

        public async Task Ready()
        {
            await InitializedSempahore.WaitAsync();
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
                                // Do not initialize bots
                                var sentimentScores = await this.sentimentService.AnalyzeMessage(messageSet.Where(z => !z.Author.IsBot).Select(x => new SentimentMessage(x)).ToList());
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
                InitializedSempahore.Release();
            }
        }

        public async Task JoinedGuild(SocketGuild guild)
        {
            await this.Ready();
        }

        public async Task UserVoiceStateUpdated(SocketUser user, SocketVoiceState from, SocketVoiceState to)
        {
            if(user.Id == discordClient.CurrentUser.Id)
            {
                var guildUser = user as SocketGuildUser;
                if(to.VoiceChannel != null && guildUser != null)
                {

                    // On join, populate all the existing users
                    foreach(var cu in to.VoiceChannel.Users)
                    {
                        UserChannels[cu.Id] = new VoiceStream(to.VoiceChannel.Id);
                        SpeakingSemaphores[cu.Id] = new SemaphoreSlim(1);
                    }

                    Console.WriteLine(user.GetType());
                    Console.WriteLine(to.GetType());
                    guildUser.Guild.AudioClient.Connected += AudioConnected;
                    guildUser.Guild.AudioClient.StreamCreated += StreamCreated;
                    guildUser.Guild.AudioClient.SpeakingUpdated += SpeakingUpdated;
                }
                Console.WriteLine($"Bot voice state change: {user.Username}, to: {from}, from: {to}");
            }
            else
            {
                // Once joined, track changes as they come
                if(to.VoiceChannel?.Id == null)
                {
                    UserChannels.Remove(user.Id);
                    SpeakingSemaphores.Remove(user.Id);
                }
                else
                {
                    UserChannels[user.Id] = new VoiceStream(to.VoiceChannel.Id);
                    SpeakingSemaphores[user.Id] = new SemaphoreSlim(1);
                }
            }
        }

        private async Task SpeakingUpdated(ulong authorId, bool isSpeaking)
        {
            VoiceStream currentValue;
            if(UserChannels.TryGetValue(authorId, out currentValue))
            {
                // Was speaking, is now stopped
                if(currentValue.IsSpeaking == true && isSpeaking == false)
                {
    #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    Task.Factory.StartNew(async () =>
                    {
                        try
                        {
                            await SpeakingSemaphores[authorId].WaitAsync();

                            var speechToText = await this.voiceToTextService.ProcessVoiceToText(currentValue.Stream);
                            if(!string.IsNullOrEmpty(speechToText))
                            {
                                var sentiment = await this.sentimentService.AnalyzeMessage(new List<SentimentMessage>()
                                {
                                    new SentimentMessage() {
                                            Author = this.discordClient.GetUser(authorId).Username,
                                            AuthorId = authorId,
                                            ChannelId = this.discordClient.GetChannel(this.UserChannels[authorId].ChannelId).Id,
                                            Timestamp = DateTime.UtcNow,
                                            Content = speechToText
                                    }
                                });

                                // var result = await this.sentimentHistoryService.StoreMessage(sentiment);

                            }
                            currentValue.Stream.SetLength(0);
                        }
                        finally
                        {
                            SpeakingSemaphores[authorId]?.Release();
                        }
                    });
    #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    
                }
                currentValue.IsSpeaking = isSpeaking;
            }
            Console.WriteLine($"Speaking Updated for {authorId}, VoiceMemoryStream: {UserChannels[authorId]}, isSpeaking: {isSpeaking}");
        }

        private async Task StreamCreated(ulong id, AudioInStream audio)
        {
            Console.WriteLine($"Stream created: {id}, {audio}");
            var streamCancelToken = new CancellationToken();
            await Task.Factory.StartNew(async () =>
            {
                try
                {
                    var stream = UserChannels[id].Stream;
                    var writer = new RawSourceWaveStream(stream, new WaveFormat(46000, 2));
                    while (!streamCancelToken.IsCancellationRequested)
                    {
                        RTPFrame frame = await audio.ReadFrameAsync(streamCancelToken);
                        stream.Write(frame.Payload, 0, frame.Payload.Length);
                        // Console.WriteLine($"AudioFrameReceived, {frame.Payload.Length}, {frame.Sequence}");
                    }
                }
                catch (Exception e) { Console.WriteLine(e); }
            }, streamCancelToken);
        }

        private async Task AudioConnected()
        {
            Console.WriteLine("Audio connected");
        }
    }
}
