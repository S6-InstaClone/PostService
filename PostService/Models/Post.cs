using System.ComponentModel.DataAnnotations;

namespace PostService.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(36)] // UUID length
        public string UserId { get; set; } = string.Empty;  // Keycloak sub claim (UUID)

        [MaxLength(150)]
        public string? Username { get; set; }  // Keycloak preferred_username 

        [Required]
        [MaxLength(2200)]
        public string Caption { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}