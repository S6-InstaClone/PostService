using PostService.Dtos;
using Azure.Storage.Blobs;

namespace PostService.Persistence
{
    public class BlobService
    {
        private readonly BlobServiceClient _client;

        public BlobService()
        {
            // Azurite local connection string
            var connectionString = "UseDevelopmentStorage=true";
            _client = new BlobServiceClient(connectionString);
        }

        //public async Task<string> UploadProfilePictureAsync(UploadProfilePictureDto dto)
        //{
        //    var container = _client.GetBlobContainerClient("profile-pictures");
        //    await container.CreateIfNotExistsAsync();

        //    var blob = container.GetBlobClient($"{dto.Id}/{dto.File.FileName}");

        //    using var stream = dto.File.OpenReadStream();
        //    await blob.UploadAsync(stream, overwrite: true);

        //    return blob.Uri.ToString(); // This is what you store in the DB
        //}
    }
}
