using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;

namespace Tokki.Application.UseCases.Cloudinary.Commands.UploadAudio
{
    public class UploadAudioCommandHandler : IRequestHandler<UploadAudioCommand, OperationResult<string>>
    {
        private readonly ICloudinaryService _cloudinaryService;

        public UploadAudioCommandHandler(ICloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        public async Task<OperationResult<string>> Handle(UploadAudioCommand request, CancellationToken cancellationToken)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                await request.AudioFile.CopyToAsync(memoryStream, cancellationToken);
                var fileBytes = memoryStream.ToArray();

                var originalName = Path.GetFileNameWithoutExtension(request.AudioFile.FileName);
                var fileName = $"{originalName}_{Guid.NewGuid()}";

                var url = await _cloudinaryService.UploadAudioAsync(fileBytes, fileName, request.FolderName);

                return OperationResult<string>.Success(url);
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Failure($"Lỗi upload Audio: {ex.Message}");
            }
        }
    }
}
