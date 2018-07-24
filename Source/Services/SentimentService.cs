using DDBot.Configuration;
using DDBot.Models;
using Discord.WebSocket;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DDBot.Services
{
    public class SentimentService
    {
        private readonly Secrets secrets;
        private readonly ITextAnalyticsAPI client;

        public SentimentService(Secrets secrets)
        {
            this.secrets = secrets;
            this.client = new TextAnalyticsAPI(new ApiKeyServiceClientCredentials(secrets.AzureSentimentKey));
            this.client.AzureRegion = AzureRegions.Westus2;
        }

        public async Task<List<SentimentScore>> AnalyzeMessage(List<SocketMessage> messages)
        {
            var inputs = new List<MultiLanguageInput>();
            var i = 0;
            foreach(var msg in messages)
            {
                inputs.Add(new MultiLanguageInput("en", (i++).ToString(), msg.Content));
            }

            SentimentBatchResult result = await client.SentimentAsync(new MultiLanguageBatchInput(inputs));
            List<SentimentScore> output = new List<SentimentScore>();


            i = 0;
            if(result?.Documents != null)
            {
                foreach(var document in result.Documents)
                {
                    var latestScore = new SentimentScore()
                    {
                        Author = messages[i].Author.Username,
                        AuthorId = messages[i].Author.Id,
                        ChannelId = messages[i].Channel.Id,
                        Score = result.Documents[i].Score ?? -1,
                        Timestamp = messages[i].Timestamp.UtcDateTime

                    };

                    output.Add(latestScore);
                    Console.WriteLine($"Score logged: {latestScore.Author}: {latestScore.Score}. Channel: {latestScore.ChannelId}, Timestamp: {latestScore.Timestamp}");
                }

            }
            else
            {
                Console.WriteLine($"Invalid results from sentiment service: {result?.Documents}");
            }

            return output;
        }
        private class ApiKeyServiceClientCredentials : ServiceClientCredentials
        {
            public readonly string key;

            public ApiKeyServiceClientCredentials(string key)
            {
                this.key = key;
            }

            public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                return base.ProcessHttpRequestAsync(request, cancellationToken);
            }
        }
    }


}
