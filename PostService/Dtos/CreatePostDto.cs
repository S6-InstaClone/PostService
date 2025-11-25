using Microsoft.AspNetCore.Mvc;

namespace PostService.Dtos
{
    public class CreatePostDto
    {

        public string Caption { get; set; } = string.Empty;

        public IFormFile? File { get; set; }
    }
}