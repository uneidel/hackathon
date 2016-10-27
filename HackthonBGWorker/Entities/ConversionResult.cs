using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackthonBGWorker.Entities
{
    public class Phrase
    {
        public string Confidence { get; set; }
        public string Content { get; set; }
        public bool Corrected { get; set; }
        public string Offset { get; set; }
    }
    public class Entry
    {
        public int offset { get; set; }
    }
    public class DetectedPhrase
    {
        public List<Phrase> Phrase { get; set; }
    }

    public class EntityModel
    {
        public string Entity { get; set; }
        public bool Accepted { get; set; }
    }
    public class Match
    {
        public string text { get; set; }
        public List<Entry> entries { get; set; }
    }

    public class Entity
    {
        public List<Match> matches { get; set; }
        public string name { get; set; }
        public string wikipediaId { get; set; }
        public double score { get; set; }
    }
    public class KeywordModel
    {
        public string Entity { get; set; }
        public bool Accepted { get; set; }
    }
    public class ConversionResult
    {
        public ConversionResult()
        {
            DetectedPhrases = new List<DetectedPhrase>();
            Entities = new List<Entities.Entity>();
            Keywords = new List<KeywordModel>();
        }
        public string Title { get; set; }
        public string ISMUrl { get; set; }
        public string TotalLength { get; set; }
        public List<DetectedPhrase> DetectedPhrases { get; set; }
        public List<Entity> Entities { get; set; }
        public List<KeywordModel> Keywords { get; set; }

        public dynamic Celebs { get; set; }
    }

}
