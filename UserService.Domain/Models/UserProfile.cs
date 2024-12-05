using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace UserService.Domain.Models
{
	public class UserProfile
	{
        [Key]
        public int UserId { get; set; } // Foreign Key to User Id

        public string? Bio { get; set; }

        [MaxLength(255, ErrorMessage = "The website url cannot have more than 255 characters.")]
        public string? WebsiteUrl { get; set; }

        // Directly store JSON string
        public string? SocialLinks { get; set; }

        // Navigation Property back to User
        public User? User { get; set; }
    }
}

