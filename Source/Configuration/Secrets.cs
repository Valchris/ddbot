using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDBot.Configuration
{
    public class Secrets
    {
        public string AzureSentimentKey { get; set; }
        public string DiscordBotToken { get; set; }
    }
}
