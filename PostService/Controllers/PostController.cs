using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostService.Data;
using PostService.Dtos;
using PostService.Models;

namespace PostService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly PostRepository _context;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IConfiguration _config;
        private readonly ILogger<PostsController> _logger;

        public PostsController(
            PostRepository context,
            BlobServiceClient blobServiceClient,
            IConfiguration config,
            ILogger<PostsController> logger)
        {
            _context = context;
            _blobServiceClient = blobServiceClient;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Gets the user ID from the X-User-Id header (set by API Gateway after JWT validation)
        /// </summary>
        private string? GetUserId()
        {
            var userId = Request.Headers["X-User-Id"].FirstOrDefault();
            _logger.LogDebug("X-User-Id header value: {UserId}", userId);
            return userId;
        }

        /// <summary>
        /// Requires a valid user ID from the header, returns 401 if missing
        /// </summary>
        private string GetRequiredUserId()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User ID not found in request headers");
            }
            return userId;
        }

        private BlobContainerClient GetContainer()
        {
            var containerName = _config["BlobStorage:ContainerName"];
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            containerClient.CreateIfNotExists(PublicAccessType.Blob);
            return containerClient;
        }

        // GET: api/posts
        // Public endpoint - no auth required
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Post>>> GetPosts()
        {
            return await _context.Posts
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        // GET: api/posts/{id}
        // Public endpoint - no auth required
        [HttpGet("{id}")]
        public async Task<ActionResult<Post>> GetPost(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
                return NotFound();
            return post;
        }

        // GET: api/posts/my-posts
        // Private endpoint - returns posts for the authenticated user
        [HttpGet("my-posts")]
        public async Task<ActionResult<IEnumerable<Post>>> GetMyPosts()
        {
            try
            {
                var userId = GetRequiredUserId();

                var posts = await _context.Posts
                    .Where(p => p.UserId == userId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                return Ok(posts);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Authentication required" });
            }
        }

        // POST: api/posts
        // Private endpoint - requires authentication
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Post>> CreatePost([FromForm] CreatePostDto dto)
        {
            try
            {
                var userId = GetRequiredUserId();

                string? imageUrl = null;

                if (dto.File != null && dto.File.Length > 0)
                {
                    var container = GetContainer();
                    var blobName = $"{userId}/{Guid.NewGuid()}_{dto.File.FileName}";
                    var blobClient = container.GetBlobClient(blobName);

                    using var stream = dto.File.OpenReadStream();
                    await blobClient.UploadAsync(stream, overwrite: true);
                    imageUrl = blobClient.Uri.ToString();
                }

                var post = new Post
                {
                    UserId = userId,  // From gateway header, not from DTO
                    Caption = dto.Caption,
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Post {PostId} created by user {UserId}", post.Id, userId);

                return CreatedAtAction(nameof(GetPost), new { id = post.Id }, post);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Authentication required" });
            }
        }

        // PUT: api/posts/{id}
        // Private endpoint - requires authentication AND ownership
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePost(int id, [FromForm] UpdatePostDto dto)
        {
            try
            {
                var userId = GetRequiredUserId();

                var post = await _context.Posts.FindAsync(id);
                if (post == null)
                    return NotFound(new { message = "Post not found" });

                // AUTHORIZATION: Check if user owns this post
                if (post.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to update post {PostId} owned by {OwnerId}",
                        userId, id, post.UserId);
                    return Forbid(); // 403 Forbidden
                }

                post.Caption = dto.Caption;

                if (dto.File != null && dto.File.Length > 0)
                {
                    var container = GetContainer();

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(post.ImageUrl))
                    {
                        try
                        {
                            var uri = new Uri(post.ImageUrl);
                            var oldBlobName = string.Join("/", uri.Segments.Skip(2)); // Skip container name
                            var oldBlobClient = container.GetBlobClient(oldBlobName);
                            await oldBlobClient.DeleteIfExistsAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete old blob for post {PostId}", id);
                        }
                    }

                    // Upload new image
                    var newBlobName = $"{userId}/{Guid.NewGuid()}_{dto.File.FileName}";
                    var newBlobClient = container.GetBlobClient(newBlobName);

                    using var stream = dto.File.OpenReadStream();
                    await newBlobClient.UploadAsync(stream, overwrite: true);

                    post.ImageUrl = newBlobClient.Uri.ToString();
                }

                _context.Entry(post).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Post {PostId} updated by user {UserId}", id, userId);

                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Authentication required" });
            }
        }

        // DELETE: api/posts/{id}
        // Private endpoint - requires authentication AND ownership
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            try
            {
                var userId = GetRequiredUserId();

                var post = await _context.Posts.FindAsync(id);
                if (post == null)
                    return NotFound(new { message = "Post not found" });

                // AUTHORIZATION: Check if user owns this post
                if (post.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to delete post {PostId} owned by {OwnerId}",
                        userId, id, post.UserId);
                    return Forbid(); // 403 Forbidden
                }

                // Delete image if exists
                if (!string.IsNullOrEmpty(post.ImageUrl))
                {
                    try
                    {
                        var container = GetContainer();
                        var uri = new Uri(post.ImageUrl);
                        var blobName = string.Join("/", uri.Segments.Skip(2));
                        var blobClient = container.GetBlobClient(blobName);
                        await blobClient.DeleteIfExistsAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete blob for post {PostId}", id);
                    }
                }

                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Post {PostId} deleted by user {UserId}", id, userId);

                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Authentication required" });
            }
        }
    }
}