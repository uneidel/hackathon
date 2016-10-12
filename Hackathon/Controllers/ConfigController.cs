using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Table; // Namespace for Table storage types
using System.Configuration;

namespace Hackathon.Controllers
{
    public class ConfigController : ApiController
    {
        
        // POST: api/Config
        public string Post([FromBody]string burl)
        {
            var url = Base64Decode(burl);
            WebClient c = new WebClient();
            var config = c.DownloadString(url);
            return config;
        }
        private static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        
    }
}
