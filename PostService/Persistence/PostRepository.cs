using Microsoft.EntityFrameworkCore;
using PostService.Models;

namespace PostService.Data
{
    public class PostRepository : DbContext
    {
        public PostRepository(DbContextOptions<PostRepository> options) : base(options) { }

        public DbSet<Post> Posts { get; set; }

    }
}
