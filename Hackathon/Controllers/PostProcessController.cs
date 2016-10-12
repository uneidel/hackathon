using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Hackathon.Controllers
{
    public class PostProcessController : ApiController
    {
        private static readonly string _mediaServicesStorageAccountName = ConfigurationManager.AppSettings["MediaServicesStorageAccountName"];
        private static readonly string _mediaServicesStorageAccountKey = ConfigurationManager.AppSettings["MediaServicesStorageAccountKey"];
        CloudQueue ppqueue = null;
        CloudQueue statusqueue = null;
        public PostProcessController()
        {
            CloudStorageAccount mddiaservicesstorage = new CloudStorageAccount(new StorageCredentials(_mediaServicesStorageAccountName, _mediaServicesStorageAccountKey), true);
            CloudQueueClient queueClient = mddiaservicesstorage.CreateCloudQueueClient();
            ppqueue = queueClient.GetQueueReference("postprocessqueue");
            statusqueue = queueClient.GetQueueReference("statusqueue");
            ppqueue.CreateIfNotExists();
            statusqueue.CreateIfNotExists();
            
        }

        // GET: api/PostProcess
        public string Get()
        {
            CloudQueueMessage Message = statusqueue.GetMessage();

            if (Message != null)
            {
                statusqueue.DeleteMessage(Message);
                return Message.AsString;
            }
            else
                return String.Empty; 
        }
        // POST: api/PostProcess
        public void Post([FromBody]string value)
        {
            if (String.IsNullOrEmpty(value))
                return;
            CloudQueueMessage message = new CloudQueueMessage(value);
            ppqueue.AddMessage(message);
        }
    }
}
