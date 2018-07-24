using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDBot.Configuration
{
    public class Config
    {
        public int MessageRetentionInMinutes { get; set; }

        public int OnChannelJoinBootstrapMessageCount { get; set; }
    }
}
