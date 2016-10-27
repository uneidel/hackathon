using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace HackthonBGWorker
{
    internal class FaceAndCelebDetection
    {
        internal static async Task<dynamic> AnalysePictures(string containerName)
        {
            var files = GetPngInStorage(containerName);
            Dictionary<int, dynamic> infos = new Dictionary<int, dynamic>();
            var seconds = 0;  //this is currently harcoded - compare with preset.json 
            foreach (var file in files)
            {
                var result = await DetectAsync(file.ToString());
                infos.Add(seconds, result);
                seconds++;
            };
            return infos;
        }
        static async Task<dynamic> DetectAsync(string url)
        {
            string FaceApiKey = CloudConfigurationManager.GetSetting("faceapikey");
            Face[] faces = null;
            List<dynamic> celebList = new List<dynamic>();
            try
            {
                FaceServiceClient fac = new FaceServiceClient(FaceApiKey);
                faces = await fac.DetectAsync(url, true, true);
                var xx = await DetectCelebrity(url);

                //var rec = (Rectangle)(faces[0].FaceRectangle); // not convertible
                var delta = faces.Where(x => !xx.Any(y => y.Rectangle.height == x.FaceRectangle.Height
                                                       && y.Rectangle.width == x.FaceRectangle.Width
                                                       && y.Rectangle.left == x.FaceRectangle.Left
                                                       && y.Rectangle.top == x.FaceRectangle.Top))
                                                       .ToList(); // not working

                xx.ForEach(x => celebList.Add(new { Name = x.Name, Img64 = "" }));

                foreach (var f in delta)
                {
                    dynamic CelebInfo = new ExpandoObject();
                    var celebCropImg = cropImage(url, f.FaceRectangle);
                    //var celebname = await DetectCelebrity(celebCropImg);
                    CelebInfo.Img64 = ConvertToBase64(celebCropImg);
                    CelebInfo.Name = "nicht erkannt";
                    celebList.Add(CelebInfo);
                }
            }
            catch (Exception ex)
            {
                var ca = ex;
            }
            return celebList;

        }

        static async Task<List<dynamic>> DetectCelebrity(string url)
        {
            var model = "celebrities"; var cancellationTokenSource = new CancellationTokenSource();
            var client = new RestSharp.RestClient($"https://api.projectoxford.ai/");
            var request = new RestRequest($"vision/v1.0/models/{model}/analyze", Method.POST);
            request.Parameters.Clear();
            request.AddHeader("Ocp-Apim-Subscription-Key", CloudConfigurationManager.GetSetting("visionapikey"));
            var body = "{" + $"\"url\":\"{url}\"" + "}"; //dirty 
            request.AddParameter("application/json", body, ParameterType.RequestBody);

            var detectedCeleb = await client.ExecuteTaskAsync(request, cancellationTokenSource.Token);
            var celebs = JsonConvert.DeserializeObject<Celeb>(detectedCeleb.Content);
            List<dynamic> celebResult = new List<dynamic>();
            foreach (var c in celebs.result.celebrities)
            {
                dynamic celebDetail = new ExpandoObject();
                celebDetail.Name = c.name;
                celebDetail.Base64FaceRectangle = "";
                celebDetail.Rectangle = c.faceRectangle;
                celebResult.Add(celebDetail);
            }
            return celebResult;
        }
        public static byte[] imageToByteArray(System.Drawing.Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
            return ms.ToArray();
        }
        static async Task<string> DetectCelebrity(System.Drawing.Image cropImage)
        {
            var model = "celebrities";
            var cancellationTokenSource = new CancellationTokenSource();
            var client = new RestClient($"https://api.projectoxford.ai/");
            var request = new RestRequest($"vision/v1.0/models/{model}/analyze", Method.POST);
            request.Parameters.Clear();
            request.AddHeader("Ocp-Apim-Subscription-Key", CloudConfigurationManager.GetSetting("visionapikey"));

            request.AddFileBytes("celeb", imageToByteArray(cropImage), "celeb.jpg", "application/octet-stream");
            var detectedCeleb = await client.ExecuteTaskAsync(request, cancellationTokenSource.Token);
            var celebs = JsonConvert.DeserializeObject<Celeb>(detectedCeleb.Content);

            string sName = "";
            if (celebs.result.celebrities.Count > 0)
                sName = celebs.result.celebrities[0].name;
            return sName;
        }

        private static System.Drawing.Image cropImage(string imgurl, Microsoft.ProjectOxford.Face.Contract.FaceRectangle cropArea)
        {

            Rectangle r = new Rectangle(cropArea.Left, cropArea.Top, cropArea.Width, cropArea.Height);
            WebClient wc = new WebClient();
            byte[] bytes = wc.DownloadData(imgurl);
            MemoryStream ms = new MemoryStream(bytes);
            System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
            Bitmap bmpImage = new Bitmap(img);
            Bitmap newBitmap = bmpImage.Clone(r, bmpImage.PixelFormat);
#if DEBUG
            newBitmap.GetThumbnailImage(150, 150, null, IntPtr.Zero).Save("c:\\temp\\celeb.jpg");

#endif
            return newBitmap;
        }
        private static string ConvertToBase64(System.Drawing.Image img)
        {
            string base64String = "";
            using (MemoryStream m = new MemoryStream())
            {
                img.GetThumbnailImage(150, 150, null, IntPtr.Zero).Save(m, ImageFormat.Jpeg);
                byte[] imageBytes = m.ToArray();

                // Convert byte[] to Base64 String
                base64String = Convert.ToBase64String(imageBytes);

            }
            return base64String;
        }


        private static List<Uri> GetPngInStorage(string containername)
        {

            List<Uri> files = new List<Uri>();
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Convert.ToString(System.Configuration.ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"]));

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();


            CloudBlobContainer container = blobClient.GetContainerReference(containername);
            BlobContainerPermissions containerPermissions = new BlobContainerPermissions();
            containerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            // Loop over items within the container and output the length and URI.
            foreach (IListBlobItem item in container.ListBlobs(null, false))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    if (blob.Uri.ToString().EndsWith("png") || blob.Uri.ToString().EndsWith("jpg"))
                    {
                        Console.WriteLine("Block blob of length {0}: {1}", blob.Properties.Length, blob.Uri);
                        files.Add(blob.Uri);
                    }
                }

            }
            return files;
        }
    }

    public class Metadata
    {
        public int width { get; set; }
        public int height { get; set; }
        public string format { get; set; }
    }

    public class FaceRectangle
    {
        public int left { get; set; }
        public int top { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class Celebrity
    {
        public string name { get; set; }
        public FaceRectangle faceRectangle { get; set; }
        public double confidence { get; set; }
    }

    public class Result
    {
        public List<Celebrity> celebrities { get; set; }
    }

    public class Celeb
    {
        public string requestId { get; set; }
        public Metadata metadata { get; set; }
        public Result result { get; set; }
    }
}
