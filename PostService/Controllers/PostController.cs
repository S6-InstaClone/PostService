using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using PostService.Business;
using PostService.Dtos;
using PostService.Models;

namespace PostService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly PostsService _postsService;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IConfiguration _config;
        private readonly ILogger<PostsController> _logger;

        public PostsController(
            PostsService postsService,
            BlobServiceClient blobServiceClient,
            IConfiguration config,
            ILogger<PostsController> logger)
        {
            _postsService = postsService;
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
        /// Gets the username from the X-User-Name header (set by API Gateway after JWT validation)
        /// </summary>
        private string? GetUsername()
        {
            var username = Request.Headers["X-User-Name"].FirstOrDefault();
            _logger.LogDebug("X-User-Name header value: {Username}", username);
            return username;
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
        // NOW CACHED via PostsService
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Post>>> GetPosts(CancellationToken ct)
        {
            var posts = await _postsService.GetAllPostsAsync(page: 1, pageSize: 50, ct);
            return Ok(posts);
        }

        // GET: api/posts/{id}
        // Public endpoint - no auth required
        // NOW CACHED via PostsService
        [HttpGet("{id}")]
        public async Task<ActionResult<Post>> GetPost(int id, CancellationToken ct)
        {
            var post = await _postsService.GetPostByIdAsync(id, ct);
            if (post == null)
                return NotFound();
            return Ok(post);
        }

        // GET: api/posts/my-posts
        // Private endpoint - returns posts for the authenticated user
        // NOW CACHED via PostsService
        [HttpGet("my-posts")]
        public async Task<ActionResult<IEnumerable<Post>>> GetMyPosts(CancellationToken ct)
        {
            try
            {
                var userId = GetRequiredUserId();
                var posts = await _postsService.GetPostsByUserAsync(userId, ct);
                return Ok(posts);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Authentication required" });
            }
        }

        // POST: api/posts
        // Private endpoint - requires authentication
        // Invalidates cache via PostsService
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Post>> CreatePost([FromForm] CreatePostDto dto, CancellationToken ct)
        {
            try
            {
                var userId = GetRequiredUserId();
                var username = GetUsername();
                string? imageUrl = null;

                // Handle image upload
                if (dto.File != null && dto.File.Length > 0)
                {
                    var container = GetContainer();
                    var blobName = $"{userId}/{Guid.NewGuid()}_{dto.File.FileName}";
                    var blobClient = container.GetBlobClient(blobName);

                    using var stream = dto.File.OpenReadStream();
                    await blobClient.UploadAsync(stream, overwrite: true, ct);
                    imageUrl = blobClient.Uri.ToString();
                }

                // Create post via service (handles caching)
                var post = await _postsService.CreatePostAsync(
                    userId,
                    username,
                    dto.Caption,
                    imageUrl,
                    ct);

                _logger.LogInformation("Post {PostId} created by user {UserId} ({Username})",
                    post.Id, userId, username);

                return CreatedAtAction(nameof(GetPost), new { id = post.Id }, post);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Authentication required" });
            }
        }

        // PUT: api/posts/{id}
        // Private endpoint - requires authentication AND ownership
        // Invalidates cache via PostsService
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePost(int id, [FromForm] UpdatePostDto dto, CancellationToken ct)
        {
            try
            {
                var userId = GetRequiredUserId();

                // Get post for ownership check and old image URL
                var existingPost = await _postsService.GetPostForWriteAsync(id, ct);
                if (existingPost == null)
                    return NotFound(new { message = "Post not found" });

                // Check ownership
                if (existingPost.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to update post {PostId} owned by {OwnerId}",
                        userId, id, existingPost.UserId);
                    return Forbid();
                }

                string? newImageUrl = existingPost.ImageUrl;

                // Handle image upload if provided
                if (dto.File != null && dto.File.Length > 0)
                {
                    var container = GetContainer();

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingPost.ImageUrl))
                    {
                        try
                        {
                            var uri = new Uri(existingPost.ImageUrl);
                            var oldBlobName = string.Join("/", uri.Segments.Skip(2));
                            var oldBlobClient = container.GetBlobClient(oldBlobName);
                            await oldBlobClient.DeleteIfExistsAsync(cancellationToken: ct);
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
                    await newBlobClient.UploadAsync(stream, overwrite: true, ct);
                    newImageUrl = newBlobClient.Uri.ToString();
                }

                // Update via service (handles caching)
                var updatedPost = await _postsService.UpdatePostAsync(
                    id,
                    userId,
                    dto.Caption,
                    newImageUrl,
                    ct);

                if (updatedPost == null)
                    return NotFound(new { message = "Post not found or access denied" });

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
        // Invalidates cache via PostsService
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id, CancellationToken ct)
        {
            try
            {
                var userId = GetRequiredUserId();

                // Delete via service (handles caching and ownership check)
                var deletedPost = await _postsService.DeletePostAsync(id, userId, ct);

                if (deletedPost == null)
                    return NotFound(new { message = "Post not found or access denied" });

                // Clean up blob storage
                if (!string.IsNullOrEmpty(deletedPost.ImageUrl))
                {
                    try
                    {
                        var container = GetContainer();
                        var uri = new Uri(deletedPost.ImageUrl);
                        var blobName = string.Join("/", uri.Segments.Skip(2));
                        var blobClient = container.GetBlobClient(blobName);
                        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete blob for post {PostId}", id);
                    }
                }

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