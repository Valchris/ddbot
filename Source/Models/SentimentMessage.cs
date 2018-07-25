using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDBot.Models
{
    public class SentimentMessage
    {
        public string Author { get; set; }
        public ulong AuthorId { get; set; }

        public ulong ChannelId { get; set; }

        public DateTime Timestamp { get; set; }

        public string Content { get; set; }

        public SentimentMessage(IMessage msg) : this()
        {
            Author = msg.Author.Username;
            AuthorId = msg.Author.Id;
            ChannelId = msg.Channel.Id;
            Timestamp = msg.Timestamp.UtcDateTime;
            Content = msg.Content;
        }

        public SentimentMessage() { }

    }
}
