using System.ComponentModel.DataAnnotations;

namespace PostService.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }  // Could come from Auth Service

        [Required]
        [MaxLength(2200)]
        public string Caption { get; set; }

        public string? ImageUrl { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
