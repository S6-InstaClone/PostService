using Microsoft.AspNetCore.Mvc;

namespace PostService.Dtos
{
    public class CreatePostDto
    {
        public int UserId { get; set; }
        public string  Caption { get; set; }
        public IFormFile File { get; set; }
    }
}
