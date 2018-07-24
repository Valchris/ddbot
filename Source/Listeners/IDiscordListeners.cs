using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDBot.Listeners
{
    public interface IDiscordListeners
    {
        Task Log(LogMessage msg);

        Task MessageReceived(SocketMessage message);

        Task Ready();
        Task JoinedGuild(SocketGuild guild);
        Task UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3);
    }
}
