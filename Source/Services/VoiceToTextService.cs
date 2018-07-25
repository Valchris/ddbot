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
        private const int InputRate = 16000;

        public VoiceToTextService()
        {
            var speechModelsDir = Path.Combine(Directory.GetCurrentDirectory(), "SpeechModels");
            config = new Syn.Speech.Api.Configuration()
            {
                AcousticModelPath = speechModelsDir,
                DictionaryPath = Path.Combine(speechModelsDir, "cmudict-en-us.dict"),
                LanguageModelPath = Path.Combine(speechModelsDir, "en-us.lm.dmp"),
                SampleRate = InputRate
            };
            this.speechRecognizer = new StreamSpeechRecognizer(config);
        }

        public async Task<string> ProcessVoiceToText(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var wavStream = new RawSourceWaveStream(stream, new WaveFormat(64000, 2));
            // WaveFileWriter.CreateWaveFile("fn-source.wav", wavStream);

            stream.Seek(0, SeekOrigin.Begin);
            var newFormat = new WaveFormat(InputRate, 16, 1);
            WaveFormatConversionStream cs = new WaveFormatConversionStream(newFormat, wavStream);

            // var fn = "a-" + Guid.NewGuid() + ".wav";
            // WaveFileWriter.CreateWaveFile(fn, cs);
            cs.Seek(0, SeekOrigin.Begin);
            speechRecognizer.StartRecognition(cs);
            var result = speechRecognizer.GetResult();
            speechRecognizer.StopRecognition();
            Console.WriteLine($"STT: {result?.GetHypothesis()}");

            return result?.GetHypothesis();
        }

    }
}
