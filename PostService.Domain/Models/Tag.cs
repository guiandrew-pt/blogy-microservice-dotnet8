using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PostService.Domain.Models
{
	public class Tag
	{
        [BsonId] // Marks this as the MongoDB _id field
        [BsonRepresentation(BsonType.ObjectId)] // Ensures it is serialized/deserialized as an ObjectId
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString(); // Default to a new ObjectId

        [Required, MaxLength(30, ErrorMessage = "The name cannot have more than 30 characters.")]
        public string? Name { get; set; }

        // Reference back to Posts (Many-to-Many relationship)
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string>? PostIds { get; set; } = new List<string>(); // Use `ObjectId`-formatted strings

        // Optional: Additional metadata
        public int UsageCount { get; set; } = 0; // Tracks how often the tag is used

        // Optional metadata for better tracking
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow; // When the tag was created
        public DateTime? LastUsedDate { get; set; } // Tracks when the tag was last used

        // Optional description for the tag
        public string? Description { get; set; } // Short description of the tag
    }
}

