using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;

namespace Tokki.Application.UseCases.Cloudinary.Commands.UploadVocabularyImage
{
    public class UploadVocabularyImageCommandHandler : IRequestHandler<UploadVocabularyImageCommand, OperationResult<string>>
    {
        private readonly ICloudinaryService _cloudinaryService;

        public UploadVocabularyImageCommandHandler(ICloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        public async Task<OperationResult<string>> Handle(UploadVocabularyImageCommand request, CancellationToken cancellationToken)
        {
            const string folderName = "tokki/vocab-image";
            try
            {
                var url = await _cloudinaryService.UploadImageAsync(request.File, folderName);

                if (string.IsNullOrEmpty(url))
                {
                    return OperationResult<string>.Failure("Upload ảnh lên Cloudinary thất bại.", 500);
                }

                return OperationResult<string>.Success(url);
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Failure($"Lỗi hệ thống: {ex.Message}", 500);
            }
        }
    }
}