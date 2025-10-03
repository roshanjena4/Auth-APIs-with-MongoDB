using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApi.Models
{
    public class User
    {
        [BsonId] // This attribute makes this property the primary key
        [BsonRepresentation(BsonType.ObjectId)] // Represents it as an ObjectId in MongoDB
        public string? Id { get; set; }

        [BsonElement("Username")] // Maps this property to the "Username" field in MongoDB
        public string Username { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public byte[]? ProfileImage { get; set; }
    }
}