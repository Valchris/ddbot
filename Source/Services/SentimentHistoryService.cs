using DDBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDBot.Services
{
    public class SentimentHistoryService
    {
        private readonly List<SentimentScore> scores;

        public SentimentHistoryService()
        {
            this.scores = new List<SentimentScore>();
        }

        public void StoreMessage(SentimentScore score)
        {
            this.scores.Add(score);
            // Add logic for cold storage here

            // Add remove message logic for >24h
        }

    }
}
