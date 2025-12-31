using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Cloudinary.Commands.UploadImage
{
    public class UploadImageCommandValidator : AbstractValidator<UploadImageCommand>
    {
        public UploadImageCommandValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.File)
                .NotNull().WithMessage("Vui lòng chọn file ảnh.")
                .Must(f => f.Length > 0).WithMessage("File ảnh không được rỗng.")

                .Must(f => f.Length <= 5 * 1024 * 1024)
                .WithMessage("Kích thước ảnh không được vượt quá 5MB.")

                .Must(IsAllowedContentType)
                .WithMessage("Định dạng không hợp lệ. Chỉ chấp nhận: .jpg, .jpeg, .png, .webp");
        }

        private bool IsAllowedContentType(IFormFile file)
        {
            if (file is null) return false;

            var allowedContentTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/jpg",
                "image/webp"
            };

            return allowedContentTypes.Contains(file.ContentType.ToLower());
        }
    }
}
