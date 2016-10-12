using HackthonBGWorker.Entities;
using Newtonsoft.Json;
using speech_to_text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace HackthonBGWorker
{
    internal class EntityAndKeyWords
    {
        internal async static Task ProcessKeyWordsAndEntities(ConversionResult result)
        {
            var totalString = ""; var count = 0; var sid = 0;
            //Prepare Entities for TextAnalysis
            KeyPhraseEntity r = new KeyPhraseEntity();
            
            r.documents = new List<KeyPhraseDocument>();
            var realtotalstring = String.Empty;

            for (int i =0; i < result.DetectedPhrases.Count; i++)
            {
            
                var e = result.DetectedPhrases[i];
                totalString += e.Phrase.FirstOrDefault().Content;
                count++;
                if (count == 10 || i == (result.DetectedPhrases.Count-1))
                {
                    r.documents.Add(new KeyPhraseDocument() { id = sid.ToString(), language = "de", text = totalString });
                    // 10 KB Limit so do it hier
                    var el = await MakeEntityLinkingCall(totalString);
                    result.Entities.AddRange(el.entities);
                    count = 0; totalString = ""; sid++;
                }
            }
            TAResult foo = await MakeKeywordCall(r);
            foreach (var kf in foo.documents.FirstOrDefault().keyPhrases)
            {
                var em = new KeywordModel();
                em.Entity = kf; result.Keywords.Add(em);
            }
        }
        internal static async Task<TAResult> MakeKeywordCall(dynamic r)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key",
                System.Configuration.ConfigurationManager.AppSettings["TextAnalysisKey"].ToString());

            var uri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/keyPhrases";

            HttpResponseMessage response;
            var json = JsonConvert.SerializeObject(r);
            byte[] byteData = Encoding.UTF8.GetBytes(json);
            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
            }
            var byties = await response.Content.ReadAsByteArrayAsync();
            var responsestuff = Encoding.UTF8.GetString(byties);
            var data = JsonConvert.DeserializeObject<TAResult>(responsestuff);
            return data;
        }


        internal static async Task<EntityLinkingObject> MakeEntityLinkingCall(string r)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key",
                System.Configuration.ConfigurationManager.AppSettings["EntityLinkingKey"].ToString());

            var uri = "https://api.projectoxford.ai/entitylinking/v1.0/link";

            HttpResponseMessage response;
            var json = JsonConvert.SerializeObject(r);
            byte[] byteData = Encoding.UTF8.GetBytes(json);
            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                response = await client.PostAsync(uri, content);
            }
            var byties = await response.Content.ReadAsByteArrayAsync();
            var responsestuff = Encoding.UTF8.GetString(byties);
            var data = JsonConvert.DeserializeObject<EntityLinkingObject>(responsestuff);
            return data;
        }
    }
}
