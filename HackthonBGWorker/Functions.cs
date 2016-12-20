using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Xml.Linq;
using Microsoft.WindowsAzure.Storage.Queue;
using NAudio.Wave;
using System.Diagnostics;
using speech_to_text;
using static HackthonBGWorker.StatusQueueHelper;
using HackthonBGWorker.Entities;
using Newtonsoft.Json;
using System.Configuration;
using System.Xml;
using Newtonsoft.Json.Linq;

namespace HackthonBGWorker
{
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        static CloudStorageAccount storageAccount = null;
        public async static void ProcessQueueMessage([QueueTrigger("postprocessqueue")] string message, TextWriter log)
        {
            //message contains containername of Blob Storage
            log.WriteLine(message);
            var paket = JObject.Parse(message);
            var IncomingUrl = Convert.ToString(paket["IncomingUrl"]);
            var AssetUrl = Convert.ToString(paket["AssetUrl"]);
            var LocatorUrl = Convert.ToString(paket["LocatorUrl"]);
            var containerName = AssetUrl.Substring(AssetUrl.LastIndexOf("/") + 1);
            
            try
            {
                // Access *.manifest.xml for concrete Filenames
                storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString);

                // Create the blob client.
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(containerName);
                var xdoc = GetManiFest(container);
                // Get Name of Audio File
                var audioFile = xdoc.Descendants().SingleOrDefault(p => p.Name.LocalName == "AudioTrack").Parent.Parent.Attribute("Name").Value; //DIRTY but works
                addStatusToQueue(storageAccount, JobStatus.Processing, "Manifest parsed.");
                var file = container.GetBlockBlobReference(audioFile);
                var workingDir = Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                var fullDownloadPath = System.IO.Path.Combine(workingDir, audioFile);
                Directory.CreateDirectory(workingDir);
                
                file.DownloadToFile(fullDownloadPath, FileMode.Create);
                addStatusToQueue(storageAccount, JobStatus.Processing, "AudioFile downloaded");
                var wavFile = convertToWav(fullDownloadPath);
                addStatusToQueue(storageAccount, JobStatus.Processing, "AudioFile converted");
                //AdvAudioSplitter.OptimizeWav(wavFile);

                // Create output Result
                var result = new ConversionResult();
                result.Title = IncomingUrl;
                result.ISMUrl = LocatorUrl;//container.ListBlobs().OfType<CloudBlockBlob>().Where(b => b.Name.EndsWith("ism")).FirstOrDefault().Uri.ToString() + "/manifest&format=smooth";
                // like AMSExplorer function GetStreamingUris but quick and Dirty

                result.TotalLength = GetWavFileDuration(wavFile).ToString("g");

                WavFileUtils.SplitAudio(wavFile);
                addStatusToQueue(storageAccount, JobStatus.Processing, "AudioFiles optimized");
                
                AnalyseAudio(result, workingDir);
                addStatusToQueue(storageAccount, JobStatus.Processing, "AudioFiles analyzed");

                try
                {
                    await EntityAndKeyWords.ProcessKeyWordsAndEntities(result);
                    addStatusToQueue(storageAccount, JobStatus.Processing, "Entities and Keywords extracted.");
                }
                catch(Exception ex)
                {
                    addStatusToQueue(storageAccount, JobStatus.Warning, "Entity and Keyword extraction failed");
                }

                // Analyse Pictures
                if (!String.IsNullOrEmpty(CloudConfigurationManager.GetSetting("faceapikey")))
                {
                    result.Celebs = await FaceAndCelebDetection.AnalysePictures(containerName);
                }
                var urisas = UploadResultAndSetPublic(container, result);
                //Getting Lazy  Friday afternoon
                SetContainerPublic(container);
                addStatusToQueue(storageAccount, JobStatus.Processing, "PostProcessing Finished");
                addStatusToQueue(storageAccount, JobStatus.Finished, urisas);

            }
            catch (Exception ex)
            {
                addStatusToQueue(storageAccount, JobStatus.Error, ex.ToString());
            }
        }
        private static void SetContainerPublic(CloudBlobContainer container)
        {  
            container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Container });
        }
        private static string UploadResultAndSetPublic(CloudBlobContainer container, ConversionResult result)
        {
            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference("result.json");
            var json = JsonConvert.SerializeObject(result);
            blockBlob.UploadText(json, Encoding.UTF8);
         SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5);
            sasConstraints.SharedAccessExpiryTime = DateTime.UtcNow.AddDays(14);
            sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write;

            //Generate the shared access signature on the blob, setting the constraints directly on the signature.
            string sasBlobToken = blockBlob.GetSharedAccessSignature(sasConstraints);

            //Return the URI string for the container, including the SAS token.
            return blockBlob.Uri + sasBlobToken;

        }
        private static XDocument GetManiFest(CloudBlobContainer container)
        {
            Stream xml = new MemoryStream();
            foreach (IListBlobItem item in container.ListBlobs(null, false))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    if (blob.Uri.ToString().EndsWith("manifest.xml")) {
                        blob.DownloadToStream(xml);
                        break;
                    }
                }
            }
            xml.Position = 0;
            XDocument xmldoc = XDocument.Load(xml);
            return xmldoc;
        }

        private static string convertToWav(string fullPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(fullPath);
            var path = Path.GetDirectoryName(fullPath);
            var newFileName = $"{path}/{fileName}.wav";
            using (var reader = new MediaFoundationReader(fullPath))
            {
                WaveFileWriter.CreateWaveFile(newFileName, reader);
            }
            return newFileName;
        }

        private static void SplitAudio(string fullPath)
        {
            // sox supports noisered 
            var startInfo = new ProcessStartInfo("sox\\sox.exe",
               String.Format("{0} {1} silence 0 1 0.33 1% : newfile : restart", fullPath, 
                                Path.Combine(Path.GetDirectoryName(fullPath), "split.wav")));

             //[ -l] above_periods[duration threshold[d |%]][below_periods duration threshold[d |%]]
            startInfo.UseShellExecute = false;

            var p = Process.Start(startInfo);
            p.WaitForExit();
        }
        private static void AnalyseAudio(ConversionResult result,string workingDir)
        {
            
            var currentPosition = TimeSpan.Zero;
            var languageCode = CloudConfigurationManager.GetSetting("LanguageCode");
            var subscriptionKey = CloudConfigurationManager.GetSetting("ttsSubscriptionKey");
            // enumerate all split files and do the speech to text
           var  files = Directory.GetFiles(workingDir, "split*.wav", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var textPhrase = new SpeechEngineHelper().ConvertToText(file, languageCode, subscriptionKey);
                var splitLength = GetWavFileDuration(file);
                currentPosition += splitLength;
                if (textPhrase != null)
                {
                    var phrase = new DetectedPhrase();
                    phrase.Phrase = new List<Phrase>();
                    textPhrase.Offset = currentPosition.ToString("g");
                    phrase.Phrase.Add(textPhrase);
                    result.DetectedPhrases.Add(phrase);
                }
            }
        }
        private static TimeSpan GetWavFileDuration(string fileName)
        {
            WaveFileReader wf = new WaveFileReader(fileName);
            return wf.TotalTime;
        }
    }
}
