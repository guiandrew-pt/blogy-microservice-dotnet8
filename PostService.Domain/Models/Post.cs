using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PostService.Domain.Models
{
	public class Post
	{
        [BsonId] // Marks this as the MongoDB _id field
        [BsonRepresentation(BsonType.ObjectId)] // Ensures it is serialized/deserialized as an ObjectId
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString(); // Default to a new ObjectId

        [Required, MaxLength(30, ErrorMessage = "The title cannot have more than 30 characters.")]
        public string? Title { get; set; }

        [Required]
        public string? Content { get; set; }

        public string? ThumbnailUrl { get; set; } // Optional thumbnail for the post

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow; // Default to the current UTC time

        [Required]
        public DateTime DateUpdated { get; set; } = DateTime.UtcNow; // Tracks when the post was last updated

        [Required]
        public bool Published { get; set; }

        // Navigation Property for Comments
        public List<Comment>? Comments { get; set; } = new List<Comment>();

        // Many-to-Many Relationship with Tags (via Tag IDs)
        public List<string>? TagIds { get; set; } = new List<string>(); // Use `ObjectId`-formatted strings for Tag IDs

        // UserId as a foreign key to the MySQL User table
        // The UserId is a foreign key to the MySQL User table.
        [Required]
        public int UserId { get; set; } // References the author in the MySQL User table

        // New Metadata
        public int? ViewCount { get; set; } = 0; // Tracks how many times the post was viewed
        public int? LikeCount { get; set; } = 0; // Tracks the
    }
}

