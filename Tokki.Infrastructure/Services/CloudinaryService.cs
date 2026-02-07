using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Infrastructure.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var cloudName = configuration["CloudinarySettings:CloudName"];
            var apiKey = configuration["CloudinarySettings:ApiKey"];
            var apiSecret = configuration["CloudinarySettings:ApiSecret"];
            var account = new CloudinaryDotNet.Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                throw new Exception("File không tồn tại");

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folderName 
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Cloudinary Error: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }

        public async Task<string> UploadAudioAsync(byte[] fileBytes, string fileName, string folderName)
        {
            if (fileBytes == null || fileBytes.Length == 0)
                throw new Exception("Dữ liệu Audio rỗng");

            using var stream = new MemoryStream(fileBytes);

            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = folderName,
                Format = "mp3"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Cloudinary Audio Error: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }
        public async Task<string> UploadImageFromUrlAsync(string imageUrl, string folderName)
        {
            if (string.IsNullOrEmpty(imageUrl))
                throw new Exception("URL ảnh không được để trống");
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(imageUrl),
                Folder = folderName
            };
            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Cloudinary Error (Url Upload): {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }
        public async Task<string> UploadImageFromBytesAsync(byte[] fileBytes, string fileName, string folderName)
        {
            if (fileBytes == null || fileBytes.Length == 0)
                throw new Exception("Dữ liệu ảnh rỗng");

            using var stream = new MemoryStream(fileBytes);

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = folderName
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Cloudinary Byte Upload Error: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }
    }
}