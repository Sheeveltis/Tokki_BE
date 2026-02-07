using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Passages.Commands.UpdatePassage
{
    public class UpdatePassageCommandHandler : IRequestHandler<UpdatePassageCommand, OperationResult<string>>
    {
        private readonly IPassageRepository _passageRepository;

        public UpdatePassageCommandHandler(IPassageRepository passageRepository)
        {
            _passageRepository = passageRepository;
        }

        public async Task<OperationResult<string>> Handle(UpdatePassageCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var id = request.PassageId.Trim();

                var passage = await _passageRepository.GetByIdAsync(id, cancellationToken);
                if (passage == null)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.PassageNotFound },
                        404,
                        AppErrors.PassageNotFound.Description
                    );
                }

                // --------- Patch: "" => không update ---------
                var incomingTitle = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
                var incomingContent = string.IsNullOrWhiteSpace(request.Content) ? null : request.Content;
                var incomingImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
                var incomingAudioUrl = string.IsNullOrWhiteSpace(request.AudioUrl) ? null : request.AudioUrl.Trim(); // NEW

                var newTitle = incomingTitle ?? passage.Title;
                var newMediaType = request.MediaType ?? passage.MediaType;

                // Tính field theo patch (tạm thời), sau đó sẽ chuẩn hóa theo MediaType
                var newContent = incomingContent ?? passage.Content;
                var newImageUrl = incomingImageUrl ?? passage.ImageUrl;
                var newAudioUrl = incomingAudioUrl ?? passage.AudioUrl; // NEW

                // Không cho ra state invalid
                if (string.IsNullOrWhiteSpace(newTitle))
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.ValidationFailed },
                        400,
                        "Tiêu đề không được để trống."
                    );
                }

                // Validate theo mediaType sau cùng
                if (newMediaType == PassageMediaType.Text)
                {
                    if (string.IsNullOrWhiteSpace(newContent))
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Loại Văn bản bắt buộc phải có nội dung."
                        );
                    }
                }
                else if (newMediaType == PassageMediaType.Image)
                {
                    if (string.IsNullOrWhiteSpace(newImageUrl))
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Loại Hình ảnh bắt buộc phải có link hình."
                        );
                    }
                }
                else if (newMediaType == PassageMediaType.Audio)
                {
                    if (string.IsNullOrWhiteSpace(newAudioUrl))
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Loại Audio bắt buộc phải có link audio."
                        );
                    }
                }
                else
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.ValidationFailed },
                        400,
                        "MediaType không hợp lệ."
                    );
                }

                // Check trùng title chỉ khi có thay đổi title
                if (incomingTitle != null && !string.Equals(incomingTitle, passage.Title, StringComparison.Ordinal))
                {
                    var titleExists = await _passageRepository.IsTitleExistsAsync(newTitle!, excludeId: id);
                    if (titleExists)
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.PassageTitleDuplicated },
                            409,
                            AppErrors.PassageTitleDuplicated.Description
                        );
                    }
                }

                // Chuẩn hóa dữ liệu theo MediaType (tránh state sai)
                switch (newMediaType)
                {
                    case PassageMediaType.Text:
                        newImageUrl = null;
                        newAudioUrl = null;
                        break;

                    case PassageMediaType.Image:
                        newContent = null;
                        newAudioUrl = null;
                        break;

                    case PassageMediaType.Audio:
                        newContent = null;
                        newImageUrl = null;
                        break;
                }

                // Apply update
                passage.Title = newTitle;
                passage.Content = newContent;
                passage.ImageUrl = newImageUrl;
                passage.AudioUrl = newAudioUrl; // NEW
                passage.MediaType = newMediaType;

                await _passageRepository.UpdateAsync(passage);
                await _passageRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(id, 200, "Cập nhật đoạn văn thành công.");
            }
            catch
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
