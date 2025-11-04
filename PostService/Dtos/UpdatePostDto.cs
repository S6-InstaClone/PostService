using Microsoft.AspNetCore.Mvc;

namespace PostService.Dtos
{
    public class UpdatePostDto
    {

        public int Id { get; set; }
        public string Caption { get; set; }

        [FromForm]
        public IFormFile File { get; set; }
    }
}
