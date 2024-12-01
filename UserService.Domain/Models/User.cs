using System.ComponentModel.DataAnnotations;

namespace UserService.Domain.Models
{
	public class User
	{
		[Key]
		public int Id { get; set; }

		[Required, MaxLength(50, ErrorMessage = "The username cannot have more than 50 characters.")]
		public string? Username { get; set; }

        [Required, EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string? PasswordHash { get; set; } // Hashed password for security

        [Required, MaxLength(50, ErrorMessage = "The first name cannot have more than 50 characters.")]
        public string? FirstName { get; set; }

        [Required, MaxLength(50, ErrorMessage = "The last name cannot have more than 50 characters.")]
        public string? LastName { get; set; }

        [MaxLength(255, ErrorMessage = "The picture file is cannot be that large.")]
        public string? ProfilePictureUrl { get; set; }

        [Required]
        public DateTime DateCreated { get; set; }

        // Navigation Property for the 1:1 relationship
        public UserProfile? Profile { get; set; }
    }
}

