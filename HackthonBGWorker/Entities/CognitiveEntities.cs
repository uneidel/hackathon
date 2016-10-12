using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackthonBGWorker.Entities
{
    public class KeyPhraseDocument
    {
        public string language { get; set; }
        public string id { get; set; }
        public string text { get; set; }
    }

    public class KeyPhraseEntity
    {
        public List<KeyPhraseDocument> documents { get; set; }
    }

    public class Document
    {
        public List<string> keyPhrases { get; set; }
        public string id { get; set; }
    }
   
    public class TAResult
    {
        public List<Document> documents { get; set; }
        public List<object> errors { get; set; }
    }
  

    
    public class EntityLinkingObject
    {
        public List<Entity> entities { get; set; }
    }
}
