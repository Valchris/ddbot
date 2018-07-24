using NAudio.Wave;
using Syn.Speech.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DDBot.Services
{
    public class VoiceToTextService
    {
        private readonly Syn.Speech.Api.Configuration config;
        private readonly StreamSpeechRecognizer speechRecognizer;

        public VoiceToTextService()
        {
            var speechModelsDir = Path.Combine(Directory.GetCurrentDirectory(), "SpeechModels");
            config = new Syn.Speech.Api.Configuration()
            {
                AcousticModelPath = speechModelsDir,
                DictionaryPath = Path.Combine(speechModelsDir, "cmudict-en-us.dict"),
                LanguageModelPath = Path.Combine(speechModelsDir, "en-us.lm.dmp"),
            };
            this.speechRecognizer = new StreamSpeechRecognizer(config);
        }

        public async Task<string> ProcessVoiceToText(Stream s, string name)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Factory.StartNew(() =>
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            {
                using (var stream = new WaveFileReader("converted.wav"))
                {
                    var newFormat = new WaveFormat(16000, 16, 1);
                    WaveFormatConversionStream cs = new WaveFormatConversionStream(newFormat, stream);

                    WaveFileWriter.CreateWaveFile("converted.wav", cs);
                    if (stream?.CanRead ?? false)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        speechRecognizer.StartRecognition(stream);
                        var result = speechRecognizer.GetResult();
                        speechRecognizer.StopRecognition();
                        Console.WriteLine($"STT: {result?.GetHypothesis()}");
                    }
                }
            });

            return null;

        }

    }
}
