using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.SpeechRecognition;
using HackthonBGWorker.Entities;

namespace speech_to_text
{
    public class SpeechEngineHelper
    {
        private DataRecognitionClient dataClient;
        private AutoResetEvent _waitingForTextConversionEvent;
        private string _text = "";
        private string _confidence;
        
        public Phrase ConvertToText(string audioFileName, string languageCode, string subscriptionKey)
        {
            dataClient = SpeechRecognitionServiceFactory.CreateDataClient(SpeechRecognitionMode.ShortPhrase, languageCode, subscriptionKey);
            // TODO Change this to Float instead of Confidence Enum !

            
            this.dataClient.OnResponseReceived += OnDataDictationResponseReceivedHandler;
            this.dataClient.OnConversationError += OnConversationErrorHandler;

            this._waitingForTextConversionEvent = new AutoResetEvent(false);
            this.SendAudioHelper(audioFileName);
            try { 
            _waitingForTextConversionEvent.WaitOne();
            }
            catch(Exception ex)
            {
                var handleme = ex.Message;
            }
            if (!String.IsNullOrEmpty(_text))
            {
                return new Phrase()
                {
                    Confidence = _confidence,
                    Content = _text
                };
            }
            return null;
        }

        private void OnConversationErrorHandler(object sender, SpeechErrorEventArgs e){}

        private void OnDataDictationResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            AppendResponseResult(e);
        }

        private void AppendResponseResult(SpeechResponseEventArgs e)
        {
            if (e.PhraseResponse.Results.Length > 0)
            {
                for (int i = 0; i < e.PhraseResponse.Results.Length; i++)
                {
                    _text += e.PhraseResponse.Results[i].DisplayText + " ";
                    _confidence = e.PhraseResponse.Results[i].Confidence.ToString();
                }
            }
            _waitingForTextConversionEvent.Set(); // in case of only one sentence, we can stop here
            if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.EndOfDictation ||
                e.PhraseResponse.RecognitionStatus == RecognitionStatus.DictationEndSilenceTimeout)
            {
                _waitingForTextConversionEvent.Set();
            }
            if (e.PhraseResponse.RecognitionStatus != RecognitionStatus.RecognitionSuccess)
            {
               //("### Status: " + e.PhraseResponse.RecognitionStatus);
            }
        }

        private void SendAudioHelper(string wavFileName)
        {
            using (FileStream fileStream = new FileStream(wavFileName, FileMode.Open, FileAccess.Read))
            {
                // Note for wave files, we can just send data from the file right to the server.
                // In the case you are not an audio file in wave format, and instead you have just
                // raw data (for example audio coming over bluetooth), then before sending up any 
                // audio data, you must first send up an SpeechAudioFormat descriptor to describe 
                // the layout and format of your raw audio data via DataRecognitionClient's sendAudioFormat() method.
                int bytesRead = 0;
                byte[] buffer = new byte[1024];

                try
                {
                    do
                    {
                        // Get more Audio data to send into byte buffer.
                        bytesRead = fileStream.Read(buffer, 0, buffer.Length);

                        // Send of audio data to service. 
                        dataClient.SendAudio(buffer, bytesRead);
                    }
                    while (bytesRead > 0);
                }
                finally
                {
                    // We are done sending audio.  Final recognition results will arrive in OnResponseReceived event call.
                    dataClient.EndAudio();
                }
            }
        }
    }
}
