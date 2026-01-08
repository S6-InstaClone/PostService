using PostService.Data;
using PostService.Dtos;
using PostService.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;
using PostService.Caching;

namespace PostService.Business
{
    public class PostsService
    {
        private readonly PostRepository _repository;
        private readonly HybridCache _cache;
        private readonly ILogger<PostsService> _logger;

        public PostsService(
            PostRepository repository,
            HybridCache cache,
            ILogger<PostsService> logger)
        {
            _repository = repository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Post?> GetPostByIdAsync(int id, CancellationToken ct = default)
        {
            var cacheKey = CacheKeys.Post(id);

            return await _cache.GetOrCreateAsync(
                cacheKey,
                async cancel =>
                {
                    _logger.LogDebug("Cache miss for post {PostId}", id);
                    return await _repository.Posts
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == id, cancel);
                },
                CacheOptions.PostCache,
                cancellationToken: ct
            );
        }

        public async Task<Post> CreatePostAsync(CreatePostDto dto, string odId)
        {
            var post = new Post
            {
                OdId = odId,
                ImageUrl = dto.ImageUrl,
                Caption = dto.Caption,
                CreatedAt = DateTime.UtcNow
            };

            _repository.Posts.Add(post);
            await _repository.SaveChangesAsync();

            // Invalidate user's posts cache
            await _cache.RemoveAsync(CacheKeys.PostsByUser(odId));

            // Invalidate feed cache (first few pages)
            for (int page = 1; page <= 3; page++)
            {
                await _cache.RemoveAsync(CacheKeys.Feed(page));
            }

            _logger.LogInformation("Created post {PostId}, invalidated caches", post.Id);
            return post;
        }

        public async Task<bool> DeletePostAsync(int id, string odId)
        {
            var post = await _repository.Posts.FindAsync(id);
            if (post == null || post.OdId != odId) return false;

            _repository.Posts.Remove(post);
            await _repository.SaveChangesAsync();

            // Invalidate caches
            await _cache.RemoveAsync(CacheKeys.Post(id));
            await _cache.RemoveAsync(CacheKeys.PostsByUser(odId));

            return true;
        }
    }
}
