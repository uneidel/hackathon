using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Table; // Namespace for Table storage types
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System.Dynamic;

namespace Hackathon.Controllers
{
    public class ConfigController : ApiController
    {
        public dynamic Get()
        {
            string connString =  $"DefaultEndpointsProtocol=https;AccountName={ConfigurationManager.AppSettings["MediaServicesStorageAccountName"]};AccountKey={ConfigurationManager.AppSettings["MediaServicesStorageAccountKey"]}";
            var storageAccount = CloudStorageAccount.Parse(connString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var serviceProperties = blobClient.GetServiceProperties();

            if (serviceProperties.Cors.CorsRules.Count > 0)
            {
                serviceProperties.Cors.CorsRules.Clear();
            }
            var cors = new CorsRule();
            cors.AllowedOrigins.Add("*");
            cors.ExposedHeaders.Add("*");
            cors.AllowedHeaders.Add("*");
            cors.AllowedMethods = CorsHttpMethods.Get | CorsHttpMethods.Put | CorsHttpMethods.Post | CorsHttpMethods.Options | CorsHttpMethods.Head;
            cors.MaxAgeInSeconds = 36000;
            serviceProperties.Cors.CorsRules.Add(cors);
            blobClient.SetServiceProperties(serviceProperties);
            
            var container = blobClient.GetContainerReference("upload");
            container.CreateIfNotExists();
            var sasToken =  GetContainerSasUri(container);
            dynamic retValue = new ExpandoObject();
            retValue.ContainerUri = container.Uri;
            retValue.SasToken = sasToken;
            return retValue;

        }

       
        // POST: api/Config
        public string Post([FromBody]string burl)
        {
            string config = String.Empty;
            try
            {
                var url = Base64Decode(burl);
                WebClient c = new WebClient();
                config = c.DownloadString(url);
            }
            catch { }
            return config;
        }
        private static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
        
        static string GetContainerSasUri(CloudBlobContainer container)
        {
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24);
            sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.List;
            string sasContainerToken = container.GetSharedAccessSignature(sasConstraints);
            return sasContainerToken;
        }
     

    }
}
