using PostService.Data;
using PostService.Dtos;
using PostService.Models;
using Microsoft.EntityFrameworkCore;
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

        /// <summary>
        /// Get all posts (feed) - cached for 60 seconds
        /// </summary>
        public async Task<IEnumerable<Post>> GetAllPostsAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
        {
            var cacheKey = CacheKeys.Feed(page);

            var posts = await _cache.GetOrCreateAsync(
                cacheKey,
                async cancel =>
                {
                    _logger.LogDebug("Cache miss for feed page {Page}", page);
                    return await _repository.Posts
                        .AsNoTracking()
                        .OrderByDescending(p => p.CreatedAt)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync(cancel);
                },
                CacheOptions.FeedCache,
                cancellationToken: ct
            );

            return posts ?? Enumerable.Empty<Post>();
        }

        /// <summary>
        /// Get single post by ID - cached for 15 minutes
        /// </summary>
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

        /// <summary>
        /// Get posts by user ID - cached for 15 minutes
        /// </summary>
        public async Task<IEnumerable<Post>> GetPostsByUserAsync(string userId, CancellationToken ct = default)
        {
            var cacheKey = CacheKeys.PostsByUser(userId);

            var posts = await _cache.GetOrCreateAsync(
                cacheKey,
                async cancel =>
                {
                    _logger.LogDebug("Cache miss for user {UserId} posts", userId);
                    return await _repository.Posts
                        .AsNoTracking()
                        .Where(p => p.UserId == userId)
                        .OrderByDescending(p => p.CreatedAt)
                        .ToListAsync(cancel);
                },
                CacheOptions.PostCache,
                cancellationToken: ct
            );

            return posts ?? Enumerable.Empty<Post>();
        }

        /// <summary>
        /// Create a new post - invalidates relevant caches
        /// </summary>
        public async Task<Post> CreatePostAsync(string userId, string? username, string caption, string? imageUrl, CancellationToken ct = default)
        {
            var post = new Post
            {
                UserId = userId,
                Username = username,
                Caption = caption,
                ImageUrl = imageUrl,
                CreatedAt = DateTime.UtcNow
            };

            _repository.Posts.Add(post);
            await _repository.SaveChangesAsync(ct);

            // Invalidate caches
            await InvalidateCachesForUserAsync(userId, ct);

            _logger.LogInformation("Created post {PostId}, invalidated caches", post.Id);
            return post;
        }

        /// <summary>
        /// Update a post - invalidates relevant caches
        /// </summary>
        public async Task<Post?> UpdatePostAsync(int id, string userId, string? caption, string? imageUrl, CancellationToken ct = default)
        {
            var post = await _repository.Posts.FindAsync(new object[] { id }, ct);

            if (post == null)
                return null;

            // Check ownership
            if (post.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to update post {PostId} owned by {OwnerId}",
                    userId, id, post.UserId);
                return null;
            }

            // Update fields
            if (caption != null)
                post.Caption = caption;
            if (imageUrl != null)
                post.ImageUrl = imageUrl;

            _repository.Entry(post).State = EntityState.Modified;
            await _repository.SaveChangesAsync(ct);

            // Invalidate caches
            await _cache.RemoveAsync(CacheKeys.Post(id), ct);
            await InvalidateCachesForUserAsync(userId, ct);

            _logger.LogInformation("Updated post {PostId}, invalidated caches", id);
            return post;
        }

        /// <summary>
        /// Delete a post - invalidates relevant caches
        /// Returns the post if found and owned by user, null otherwise
        /// </summary>
        public async Task<Post?> DeletePostAsync(int id, string userId, CancellationToken ct = default)
        {
            var post = await _repository.Posts.FindAsync(new object[] { id }, ct);

            if (post == null)
                return null;

            // Check ownership
            if (post.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to delete post {PostId} owned by {OwnerId}",
                    userId, id, post.UserId);
                return null;
            }

            _repository.Posts.Remove(post);
            await _repository.SaveChangesAsync(ct);

            // Invalidate caches
            await _cache.RemoveAsync(CacheKeys.Post(id), ct);
            await InvalidateCachesForUserAsync(userId, ct);

            _logger.LogInformation("Deleted post {PostId}, invalidated caches", id);
            return post; // Return the deleted post so controller can clean up blob
        }

        /// <summary>
        /// Get a post without ownership check (for validation before update/delete)
        /// </summary>
        public async Task<Post?> GetPostForWriteAsync(int id, CancellationToken ct = default)
        {
            // Don't use cache for write operations - need fresh data
            return await _repository.Posts.FindAsync(new object[] { id }, ct);
        }

        /// <summary>
        /// Invalidate all caches related to a user
        /// </summary>
        private async Task InvalidateCachesForUserAsync(string userId, CancellationToken ct)
        {
            // Invalidate user's posts cache
            await _cache.RemoveAsync(CacheKeys.PostsByUser(userId), ct);

            // Invalidate feed cache (first few pages)
            for (int page = 1; page <= 5; page++)
            {
                await _cache.RemoveAsync(CacheKeys.Feed(page), ct);
            }
        }
    }
}