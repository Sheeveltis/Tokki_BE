using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;

namespace Tokki.Application.UseCases.Cloudinary.Commands.UploadImage
{
    public class UploadImageCommandHandler : IRequestHandler<UploadImageCommand, OperationResult<string>>
    {
        private readonly ICloudinaryService _cloudinaryService;

        public UploadImageCommandHandler(ICloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        public async Task<OperationResult<string>> Handle(UploadImageCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var url = await _cloudinaryService.UploadImageAsync(request.File, request.FolderName);

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
