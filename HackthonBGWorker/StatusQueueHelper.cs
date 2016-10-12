using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackthonBGWorker
{
    internal class StatusQueueHelper
    {
        
        internal static void addStatusToQueue(CloudStorageAccount  storageAccount,JobStatus status, string smessage)
        {
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            var statusqueue = queueClient.GetQueueReference("statusqueue");
            var m = new StatusMessage() { message = smessage, status = status };
            CloudQueueMessage message = new CloudQueueMessage(JsonConvert.SerializeObject(m));
            statusqueue.AddMessage(message);
        }
        internal enum JobStatus
        {
            Processing, 
            Warning,
            Finished,
            Error
           
        }
        internal class StatusMessage
        {
            public JobStatus status { get; set; }
            public string message { get; set; }
        }


    }
}
