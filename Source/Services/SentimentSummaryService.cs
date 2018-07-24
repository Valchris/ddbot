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

        public void GenerateChannelAnalysis(ulong channelId)
        {

        }
    }
}
