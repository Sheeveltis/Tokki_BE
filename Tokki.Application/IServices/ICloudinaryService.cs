using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Tokki.Application.IServices
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file, string folderName);

        Task<string> UploadAudioAsync(byte[] fileBytes, string fileName, string folderName);
        Task<string> UploadImageFromUrlAsync(string imageUrl, string folderName);
        Task<string> UploadImageFromBytesAsync(byte[] fileBytes, string fileName, string folderName);
    }
}