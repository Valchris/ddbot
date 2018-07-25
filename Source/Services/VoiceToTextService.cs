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
        private Syn.Speech.Api.Configuration config;
        private StreamSpeechRecognizer speechRecognizer;
        private const int InputRate = 46000;

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

        public async Task<string> ProcessVoiceToText(Stream stream, int bitRate)
        {
            var fn = $"a-{Guid.NewGuid()}-{bitRate}.wav";
            stream.Seek(0, SeekOrigin.Begin);
            var wavStream = new RawSourceWaveStream(stream, new WaveFormat(bitRate, 2));

            // Debugging only
            // WaveFileWriter.CreateWaveFile($"{fn}-source.wav", wavStream);

            stream.Seek(0, SeekOrigin.Begin);
            var newFormat = new WaveFormat(InputRate, 1);
            WaveFormatConversionStream cs = new WaveFormatConversionStream(newFormat, wavStream);

            // Debugging only
            // WaveFileWriter.CreateWaveFile(fn, cs);
            cs.Seek(0, SeekOrigin.Begin);
            speechRecognizer.StartRecognition(cs);
            var result = speechRecognizer.GetResult();
            speechRecognizer.StopRecognition();
            return result?.GetHypothesis();
        }

    }
}
