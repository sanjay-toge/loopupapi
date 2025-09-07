using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace LoopUpAPI.Models
{
    public class Rating
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("ratedUserId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string RatedUserId { get; set; } = null!;

        [BsonElement("raterUserId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string RaterUserId { get; set; } = null!;

        public float Score { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string Relation { get; set; } = string.Empty;
        public int? KnownSince { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
