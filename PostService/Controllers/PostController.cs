using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostService.Business;
using PostService.Data;
using PostService.Dtos;
using PostService.Models;
using PostService.Persistence;
using System;

namespace PostService.Controllers
{
    namespace PostService.Controllers
    {
        [Route("api/[controller]")]
        [ApiController]
        public class PostsController : ControllerBase
        {
            private readonly PostRepository _context;
            private readonly BlobServiceClient _blobServiceClient;
            private readonly IConfiguration _config;

            public PostsController(PostRepository context, BlobServiceClient blobServiceClient, IConfiguration config)
            {
                _context = context;
                _blobServiceClient = blobServiceClient;
                _config = config;
            }

            private BlobContainerClient GetContainer()
            {
                var containerName = _config["BlobStorage:ContainerName"];
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                containerClient.CreateIfNotExists(PublicAccessType.Blob);
                return containerClient;
            }

            // GET: api/posts
            [HttpGet]
            public async Task<ActionResult<IEnumerable<Post>>> GetPosts()
            {
                return await _context.Posts.OrderByDescending(p => p.CreatedAt).ToListAsync();
            }

            // GET: api/posts/{id}
            [HttpGet("{id}")]
            public async Task<ActionResult<Post>> GetPost(int id)
            {
                var post = await _context.Posts.FindAsync(id);
                if (post == null)
                    return NotFound();
                return post;
            }

            // POST: api/posts
            [HttpPost]
            [Consumes("multipart/form-data")]
            public async Task<ActionResult<Post>> CreatePost([FromForm] CreatePostDto dto)
            {
                string? imageUrl = null;

                if (dto.File != null && dto.File.Length > 0)
                {
                    var container = GetContainer();
                    var blobName = $"{dto.UserId}/{dto.File.FileName}";
                    var blobClient = container.GetBlobClient(blobName);

                    using var stream = dto.File.OpenReadStream();
                    await blobClient.UploadAsync(stream, overwrite: true);
                    imageUrl = blobClient.Uri.ToString();
                }

                var post = new Post
                {
                    UserId = dto.UserId,
                    Caption = dto.Caption,
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPost), new { id = post.Id }, post);
            }

            // PUT: api/posts/{id}
            [HttpPut("{id}")]
            [Consumes("multipart/form-data")]
            public async Task<IActionResult> UpdatePost([FromForm] UpdatePostDto dto)
            {
                var post = await _context.Posts.FindAsync(dto.Id);
                if (post == null)
                    return NotFound();

                post.Caption = dto.Caption;

                if (dto.File != null && dto.File.Length > 0)
                {
                    var container = GetContainer();

                    // delete old image if exists
                    if (!string.IsNullOrEmpty(post.ImageUrl))
                    {
                        var oldBlobName = Path.GetFileName(new Uri(post.ImageUrl).AbsolutePath);
                        var oldBlobClient = container.GetBlobClient(oldBlobName);
                        await oldBlobClient.DeleteIfExistsAsync();
                    }

                    // upload new image
                    var newBlobName = $"{dto.Id}/{dto.File.FileName}";
                    var newBlobClient = container.GetBlobClient(newBlobName);

                    using var stream = dto.File.OpenReadStream();
                    await newBlobClient.UploadAsync(stream, overwrite: true);

                    post.ImageUrl = newBlobClient.Uri.ToString();
                }

                _context.Entry(post).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }

            // DELETE: api/posts/{id}
            [HttpDelete("{id}")]
            public async Task<IActionResult> DeletePost(int id)
            {
                var post = await _context.Posts.FindAsync(id);
                if (post == null)
                    return NotFound();

                // delete image if exists
                if (!string.IsNullOrEmpty(post.ImageUrl))
                {
                    var container = GetContainer();
                    var blobName = Path.GetFileName(new Uri(post.ImageUrl).AbsolutePath);
                    var blobClient = container.GetBlobClient(blobName);
                    await blobClient.DeleteIfExistsAsync();
                }

                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();

                return NoContent();
            }
        }
    }
}
