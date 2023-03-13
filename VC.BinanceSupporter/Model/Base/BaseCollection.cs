using System;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VC.BinanceSupporter.Model.Base
{
    [Serializable]
    public class BaseCollection : IBaseCollection
    {
        [BsonId, BsonElement("_id"), BsonRepresentation(BsonType.ObjectId), BsonIgnoreIfDefault]
        public string id { get; set; }

        [BsonIgnore]
        public int version { get; set; }

        [BsonElement("cre")]
        public long CreateDate { get; set; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

        [BsonElement("mod")]
        public long ModifiedDate { get; set; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
    }
}
