using System.ComponentModel.DataAnnotations;

namespace UserService.Domain.Models
{
	public class UserProfile
	{
        [Key]
        public int UserId { get; set; } // Foreign Key to User Id

        public string? Bio { get; set; }

        [MaxLength(255, ErrorMessage = "The website url cannot have more than 255 characters.")]
        public string? WebsiteUrl { get; set; }

        public Dictionary<string, string>? SocialLinks { get; set; }

        // Navigation Property back to User
        public User? User { get; set; }
    }
}

