using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Cloudinary.Commands.UploadAudio
{
    public class UploadAudioCommandValidator : AbstractValidator<UploadAudioCommand>
    {
        public UploadAudioCommandValidator()
        {
            RuleFor(x => x.FolderName)
                .NotEmpty().WithMessage("Tên thư mục không được để trống.");

            RuleFor(x => x.AudioFile)
                .NotNull().WithMessage("File âm thanh là bắt buộc.")
                .Must(HaveContent).WithMessage("File không được rỗng.")
                .Must(BeAValidAudioExtension).WithMessage("Định dạng file không hợp lệ. Chỉ chấp nhận: .mp3, .wav, .ogg, .m4a")
                .Must(BeValidSize).WithMessage("Dung lượng file không được vượt quá 10MB.");
        }

        private bool HaveContent(IFormFile file)
        {
            return file != null && file.Length > 0;
        }

        private bool BeAValidAudioExtension(IFormFile file)
        {
            if (file == null) return false;
            var allowedExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            return allowedExtensions.Contains(extension);
        }

        private bool BeValidSize(IFormFile file)
        {
            if (file == null) return false;
            return file.Length <= 10485760;
        }
    }
}
