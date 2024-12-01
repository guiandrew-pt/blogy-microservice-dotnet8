using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PostService.Domain.Models
{
	public class Comment
	{
        [BsonId] // Marks this as the MongoDB _id field
        [BsonRepresentation(BsonType.ObjectId)] // Ensures it is serialized/deserialized as an ObjectId
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString(); // Default to a new ObjectId

        [Required]
        public string? Content { get; set; }

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow; // Default to current UTC time

        // Reference back to Post
        [BsonRepresentation(BsonType.ObjectId)]
        public string? PostId { get; set; }  // Use ObjectId as string for Post reference

        // Relationship: Comment belongs to a User (via UserId)
        [Required]
        public int UserId { get; set; } // Reference to the MySQL User's ID

        // New Metadata
        public int LikeCount { get; set; } = 0; // Tracks the number of likes for the comment
        public bool IsEdited { get; set; } = false; // Tracks if the comment was edited
    }
}

