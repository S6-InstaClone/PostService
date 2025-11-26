using MassTransit;
using Microsoft.EntityFrameworkCore;
using PostService.Data;
using PostService.Messages;
using Azure.Storage.Blobs;

namespace PostService.Consumers
{
    /// <summary>
    /// Handles AccountDeletedEvent - deletes all posts by the deleted user (GDPR compliance)
    /// </summary>
    public class AccountDeletedConsumer : IConsumer<AccountDeletedEvent>
    {
        private readonly PostRepository _db;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountDeletedConsumer> _logger;

        public AccountDeletedConsumer(
            PostRepository db,
            BlobServiceClient blobServiceClient,
            IConfiguration configuration,
            ILogger<AccountDeletedConsumer> logger)
        {
            _db = db;
            _blobServiceClient = blobServiceClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<AccountDeletedEvent> context)
        {
            var message = context.Message;
            var userId = message.UserId;

            _logger.LogInformation(
                "GDPR Delete: Received AccountDeletedEvent for user {UserId}, reason: {Reason}",
                userId,
                message.Reason);

            try
            {
                // Find all posts by this user
                var userPosts = await _db.Posts
                    .Where(p => p.UserId == userId)
                    .ToListAsync();

                if (!userPosts.Any())
                {
                    _logger.LogInformation("GDPR Delete: No posts found for user {UserId}", userId);
                    return;
                }

                _logger.LogInformation(
                    "GDPR Delete: Found {Count} posts to delete for user {UserId}",
                    userPosts.Count,
                    userId);

                // Delete images from blob storage
                await DeleteImagesFromBlobStorageAsync(userPosts);

                // Delete posts from database
                _db.Posts.RemoveRange(userPosts);
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "GDPR Delete: Successfully deleted {Count} posts for user {UserId}",
                    userPosts.Count,
                    userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "GDPR Delete: Failed to delete posts for user {UserId}",
                    userId);
                throw; // Re-throw to trigger retry mechanism
            }
        }

        /// <summary>
        /// Delete all images associated with the user's posts from blob storage
        /// </summary>
        private async Task DeleteImagesFromBlobStorageAsync(List<PostService.Models.Post> posts)
        {
            var containerName = _configuration["BlobStorage:ContainerName"] ?? "postimages";

            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

                foreach (var post in posts.Where(p => !string.IsNullOrEmpty(p.ImageUrl)))
                {
                    try
                    {
                        // Extract blob name from URL
                        var blobName = ExtractBlobNameFromUrl(post.ImageUrl!);
                        if (!string.IsNullOrEmpty(blobName))
                        {
                            var blobClient = containerClient.GetBlobClient(blobName);
                            await blobClient.DeleteIfExistsAsync();
                            _logger.LogDebug("GDPR Delete: Deleted blob {BlobName}", blobName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "GDPR Delete: Failed to delete blob for post {PostId}, continuing with other deletions",
                            post.Id);
                        // Continue deleting other blobs even if one fails
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GDPR Delete: Error connecting to blob storage");
                // Don't throw - we still want to delete the database records
            }
        }

        /// <summary>
        /// Extract blob name from the full URL
        /// </summary>
        private string? ExtractBlobNameFromUrl(string imageUrl)
        {
            try
            {
                var uri = new Uri(imageUrl);
                // URL format: http://azurite:10000/devstoreaccount1/postimages/{userId}/{blobname}
                // We want the path after the container name
                var segments = uri.Segments;
                if (segments.Length >= 4)
                {
                    // Skip account and container, get the rest (userId/filename)
                    return string.Join("", segments.Skip(3)).TrimStart('/');
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}