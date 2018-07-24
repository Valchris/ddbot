using DDBot.Configuration;
using DDBot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace DDBot.Services
{
    public class SentimentHistoryService
    {
        private const string DataFilePath = "Data/DataStore.tsv";
        private readonly Dictionary<string, LinkedList<SentimentScore>> scores;
        private readonly Config config;

        public SentimentHistoryService(Config config)
        {
            this.scores = new Dictionary<string, LinkedList<SentimentScore>>();
            this.config = config;

            // bootstrap existing data
            if(File.Exists(DataFilePath))
            {
                var dbLines = File.ReadAllLines(DataFilePath);
                foreach(var line in dbLines)
                {
                    var parts = line.Split('\t');
                    if(parts.Length == 5)
                    {
                        var score = new SentimentScore()
                        {
                            Author = parts[0],
                            AuthorId = ulong.Parse(parts[1]),
                            ChannelId = ulong.Parse(parts[2]),
                            Score = double.Parse(parts[3]),
                            Timestamp = DateTime.Parse(parts[4])
                        };
                        var list = this.CreateListIfNeeded(score.ChannelId.ToString());
                        list.AddLast(score);
                    }
                    else
                    {
                        Console.WriteLine($"Invalid bootstrap data in {DataFilePath}, data: {line}");
                    }
                }
            }
        }

        private LinkedList<SentimentScore> CreateListIfNeeded(string channelId)
        {
            // Each Channel is stored by it's unique identifier, find or create the channel containing sentiment scores
            LinkedList<SentimentScore> channelList = null;
            if (!this.scores.TryGetValue(channelId, out channelList))
            {
                channelList = new LinkedList<SentimentScore>();
                this.scores[channelId] = channelList;
            }

            return channelList;
        }

        public void StoreMessage(List<SentimentScore> input)
        {
            foreach(var score in input)
            {
                var channelList = CreateListIfNeeded(score.ChannelId.ToString());


                // Add message to end of LinkedList
                channelList.AddLast(score);
                File.AppendAllLines(DataFilePath, new List<string>()
                {
                    score.Author + '\t' + score.AuthorId + '\t' + score.ChannelId + '\t' + score.Score + '\t' + score.Timestamp.ToUniversalTime()
                });


                // Whenever we add an old score, we prune any that exceed the config timeout duration
                while(channelList.First.Value.Timestamp < DateTime.UtcNow.AddMinutes(0 - this.config.MessageRetentionInMinutes))
                {
                    channelList.RemoveFirst();
                }
            }
        }

        public bool CheckSpamBlock(SocketMessage message)
        {
            var channelList = this.CreateListIfNeeded(message.Channel.Id.ToString());
            return channelList.Last.Value.AuthorId == message.Author.Id && channelList.Last?.Previous?.Value?.AuthorId == message.Author.Id;
        }

        public LinkedList<SentimentScore> GetMessages(ulong channelId)
        {
            return this.scores.ContainsKey(channelId.ToString()) ? this.scores[channelId.ToString()] : null;
        }

    }
}
