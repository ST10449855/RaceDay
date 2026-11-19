using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Attributes;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public FilesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Uploads an image (event banner or profile picture) to Azure Blob Storage and returns its URL.
        /// </summary>
        [HttpPost("upload")]
        [RequireRole("Organiser", "Participant")]
        public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string folder = "general")
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file was provided." });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return BadRequest(new { message = "Only .jpg, .jpeg, .png, and .webp images are allowed." });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "File size must not exceed 5 MB." });

            var connectionString = _configuration["AzureBlobStorage:ConnectionString"];
            var containerName = _configuration["AzureBlobStorage:ContainerName"] ?? "raceday-media";

            if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("YOUR_AZURE_BLOB"))
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Azure Blob Storage connection string is not configured." });

            var safeFolder = string.Concat(folder.Where(char.IsLetterOrDigit)).ToLowerInvariant();
            if (string.IsNullOrEmpty(safeFolder)) safeFolder = "general";

            var blobName = $"{safeFolder}/{Guid.NewGuid()}{ext}";

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(blobName);
            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

            return Ok(new { url = blobClient.Uri.ToString() });
        }
    }
}