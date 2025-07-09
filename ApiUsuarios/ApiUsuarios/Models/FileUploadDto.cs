// Dtos/FileUploadDto.cs
using Microsoft.AspNetCore.Http;

namespace Models
{
    public class FileUploadDto
    {
        public IFormFile File { get; set; } = null!;
    }
}
