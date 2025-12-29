using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;

namespace Tokki.Application.UseCases.Cloudinary.Commands.UploadTopicImage
{
    internal class UploadTopicImageCommandHandler : IRequestHandler<UploadTopicImageCommand, OperationResult<string>>
    {
        private readonly ICloudinaryService _cloudinaryService;

        public UploadTopicImageCommandHandler(ICloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        public async Task<OperationResult<string>> Handle(UploadTopicImageCommand request, CancellationToken cancellationToken)
        {
            const string folderName = "tokki/topic-image";
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
