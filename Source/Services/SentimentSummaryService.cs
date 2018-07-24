using DDBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDBot.Services
{
    public class SentimentSummaryService
    {
        private readonly SentimentHistoryService sentimentHistoryService;

        public SentimentSummaryService(SentimentHistoryService sentimentHistoryService)
        {
            this.sentimentHistoryService = sentimentHistoryService;
        }

        public double CalculateAverageUserSentiment(ulong channelId, ulong userId)
        {
            LinkedList<SentimentScore> scores = this.sentimentHistoryService.GetMessages(channelId);
            var userScores = scores?.Where(x => x.AuthorId == userId);

            if(userScores == null || userScores.Count() == 0)
            {
                return -1;
            }

            double acc = 0;
            foreach (var score in userScores)
            {
                acc += score.Score;
            }

            return (acc / userScores.Count());
        }

        public Dictionary<string, AggregatedScore> GenerateChannelAnalysis(ulong channelId)
        {
            Dictionary<string, AggregatedScore> output = new Dictionary<string, AggregatedScore>();
            LinkedList<SentimentScore> scores = this.sentimentHistoryService.GetMessages(channelId);
            var userGroups = scores?.GroupBy(x => x.Author);

            foreach(var userGroup in userGroups)
            {
                var dayBuckets = userGroup.GroupBy(x => x.Timestamp.DayOfYear);

                double acc = 0;
                int count = 0;
                foreach(var dayScores in dayBuckets)
                {
                    foreach(var val in dayScores)
                    {
                        acc += val.Score;
                        count += 1;   
                    }

                    var aggScore = new AggregatedScore()
                    {
                        Author = dayScores.First().Author,
                        AuthorId = dayScores.First().AuthorId,
                        Score = acc,
                        Count = count
                    };

                    output[$"{dayScores.First().Author}-{dayScores.First().Timestamp.DayOfYear}"] = aggScore;
                }
            }

            return output;
        }
    }
}
