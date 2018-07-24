using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDBot.Models
{
    public class SentimentScore
    {
        public string Author { get; set; }

        public ulong AuthorId { get; set; }

        public ulong ChannelId { get; set; }

        public double Score { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
