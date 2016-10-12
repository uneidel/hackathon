using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web;
using System.Web.Http;

namespace Hackathon.Controllers
{
    public class mediaservicesController : ApiController
    {
        // GET: api/mediaservices
        public string Get()
        {
            return "mediaservices";
        }

        // GET: api/mediaservices/5
        public string Get([FromUri]string id)
        {
            dynamic jobStatus = new ExpandoObject();
            var jobid = Base64Decode(id);
            var cachedCredentials = new MediaServicesCredentials(_mediaServicesAccountName, _mediaServicesAccountKey);
            _context = new CloudMediaContext(cachedCredentials);
            var sret = String.Empty;
            var job = _context.Jobs.Where(x => x.Id == jobid).FirstOrDefault();
            if (job.State == JobState.Processing)
                jobStatus.Status = "Processing";
            if (job.State == JobState.Error)
                jobStatus.Status = "Processing";
            if (job.State == JobState.Queued)
                jobStatus.Status = "Processing";
            if (job.State == JobState.Finished)
            {
                jobStatus.Status = "Finished";
                jobStatus.UrlSmooth = BuildStreamingURLs(_context, job.OutputMediaAssets[0]); // should be fixed
                jobStatus.AssetUri = job.OutputMediaAssets[0].Uri.ToString();
            }
            return JsonConvert.SerializeObject(jobStatus);
        }
        
        // POST: api/mediaservices
        public string Post([FromBody]string urltovideo) // content-type: application/x-www-form-urlencoded
        {
            if (String.IsNullOrEmpty(urltovideo))
                return "error";
           var cachedCredentials = new MediaServicesCredentials(_mediaServicesAccountName, _mediaServicesAccountKey);
            _context = new CloudMediaContext(cachedCredentials);
            CloudStorageAccount mddiaservicesstorage = new CloudStorageAccount(new StorageCredentials(_mediaServicesStorageAccountName, _mediaServicesStorageAccountKey), true);

            var asset = CreateAssetAndUseUrlFile(_context, mddiaservicesstorage, urltovideo);

            // Upload Media from LocalFS
            // var asset = Uploader.CreateAssetAndUploadSingleFile(_context, args[0]);

            // Encode and generate the thumbnails with preset.jspon
            var meta = EncodeWithcustomSettings(asset);
            return Base64Encode(meta);
        }

        // PUT: api/mediaservices/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/mediaservices/5
        public void Delete(int id)
        {
        }


        #region Helper
        private static readonly string _mediaServicesAccountName = ConfigurationManager.AppSettings["MediaServicesAccountName"];
        private static readonly string _mediaServicesAccountKey = ConfigurationManager.AppSettings["MediaServicesAccountKey"];

        private static readonly string _mediaServicesStorageAccountName = ConfigurationManager.AppSettings["MediaServicesStorageAccountName"];
        private static readonly string _mediaServicesStorageAccountKey = ConfigurationManager.AppSettings["MediaServicesStorageAccountKey"];

        // Field for service context.
        private static CloudMediaContext _context = null;

        private static string BuildStreamingURLs(CloudMediaContext _context, IAsset asset)
        {
            // Create a 30-day readonly access policy. 
            // You cannot create a streaming locator using an AccessPolicy that includes write or delete permissions.
           
            ILocator originLocator = null;
            IAccessPolicy policy = null;
            // Create a locator to the streaming content on an origin. 
            if (asset.Locators.Count == 0)
            {
                policy = _context.AccessPolicies.Create("Streaming policy",
                  TimeSpan.FromDays(30),
                  AccessPermissions.Read);
                originLocator = _context.Locators.CreateLocator(LocatorType.OnDemandOrigin, asset,
               policy,
               DateTime.UtcNow.AddMinutes(-5));
            }
               
                // Get a reference to the streaming manifest file from the  
                // collection of files in the asset. 
                var manifestFile = asset.AssetFiles.Where(f => f.Name.ToLower().
                                        EndsWith(".ism")).
                                        FirstOrDefault();

            // Create a full URL to the manifest file. Use this for playback
            // in streaming media clients. 
            var locator = asset.Locators[0];
            var path = locator.Path;
            var urlforsmoothStreaming = $"{path}{manifestFile.Name}/manifest";
            
            return urlforsmoothStreaming;
           
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        private static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }


        private string EncodeWithcustomSettings(IAsset asset)
        {
            // Declare a new job.
            IJob job = _context.Jobs.Create("Media Encoder Standard Job");

            // Get a media processor reference
            IMediaProcessor processor = GetLatestMediaProcessorByName("Media Encoder Standard");

            // Load the XML (or JSON) from the AppData.
            string path = HttpContext.Current.Server.MapPath("~/App_Data/preset.json");
            string configuration = File.ReadAllText(path);

            // Create a task
            ITask task = job.Tasks.AddNew("Media Encoder Standard encoding task",processor,configuration,TaskOptions.None);


            task.InputAssets.Add(asset);
            IAsset outputAsset = task.OutputAssets.AddNew("putput", AssetCreationOptions.None);
           
            // create Streaming Locator 
            
            job.Submit();
            return job.Id;
        }
        private static IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName)
        {
            var processor = _context.MediaProcessors.Where(p => p.Name == mediaProcessorName).
            ToList().OrderBy(p => new Version(p.Version)).LastOrDefault();

            if (processor == null)
                throw new ArgumentException(string.Format("Unknown media processor", mediaProcessorName));

            return processor;
        }
        private static void UploadFromUrlToBlob(Uri uri, string containerName, string fileName, CloudStorageAccount msa)
        {

            CloudBlobClient blobClient = msa.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference(containerName);
            blobContainer.CreateIfNotExists();
            var newBlockBlob = blobContainer.GetBlockBlobReference(fileName);
            var foo = newBlockBlob.StartCopyFromBlob(uri, null, null, null);
        }
        private static IAsset CreateAssetAndUseUrlFile(CloudMediaContext _context, CloudStorageAccount csa, string url)
        {
            var assetName = "Hackathon_" + DateTime.UtcNow.ToString();
            var asset = _context.Assets.Create(assetName, AssetCreationOptions.None);

            var fileName = url.Substring(url.LastIndexOf("/") + 1);
            string containerName = String.Format("asset-{0}", asset.Id.Substring(asset.Id.IndexOf("UUID:") + 5)); // Find out how to handle in a more elegant way
            var assetFile = asset.AssetFiles.Create(fileName);
            //Console.WriteLine("Created assetFile {0}", assetFile.Name);

            var accessPolicy = _context.AccessPolicies.Create(assetName, TimeSpan.FromDays(30),
                                                                AccessPermissions.Write | AccessPermissions.List);

            var locator = _context.Locators.CreateLocator(LocatorType.Sas, asset, accessPolicy);

             UploadFromUrlToBlob(new Uri(url), containerName, fileName, csa);
            locator.Delete();
            accessPolicy.Delete();

            return asset;
        }


        #endregion 
    }
}
