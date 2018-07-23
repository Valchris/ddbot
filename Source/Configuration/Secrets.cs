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
        public readonly string AzureSentimentKey;
        public readonly string DiscordKey;


        public Secrets()
        {
            JObject.Parse(File.ReadAllText(@"Data\Secrets.json"));
        }
    }
}
