using DDBot.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDBot.Models
{
    public class VoiceStream : IDisposable
    {
        public ulong ChannelId { get; set; }

        public Stream Stream { get; set; }

        public bool IsSpeaking { get; set; }

        public VoiceStream(ulong ChannelId)
        {
            this.ChannelId = ChannelId;
            this.Stream = new FileStream($"a-{Guid.NewGuid()}.wav", FileMode.Create);
            this.IsSpeaking = false;
        }

        public void Dispose()
        {
            this?.Stream.Dispose();
        }

        public override string ToString()
        {
            return $"ChannelId: {ChannelId}, IsSpeaking: {IsSpeaking}";
        }
    }
}
