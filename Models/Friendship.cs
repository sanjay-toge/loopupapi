using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace LoopUpAPI.Models
{
    public enum FriendshipStatus
    {
        None,
        Pending,
        Accepted,
        Declined,
        Blocked
    }

    public class Friendship
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonElement("requesterId")]
        public string RequesterId { get; set; }

        [BsonElement("recipientId")]
        public string RecipientId { get; set; }
        [BsonElement("status")]
        public FriendshipStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } // Change from string to DateTime
        public DateTime UpdatedAt { get; set; }
    }
}