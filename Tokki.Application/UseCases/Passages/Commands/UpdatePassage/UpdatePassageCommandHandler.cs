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

                var newTitle = incomingTitle ?? passage.Title;
                var newContent = incomingContent ?? passage.Content;
                var newImageUrl = incomingImageUrl ?? passage.ImageUrl;
                var newMediaType = request.MediaType ?? passage.MediaType;

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
                else
                {
                    if (string.IsNullOrWhiteSpace(newImageUrl))
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Loại Hình ảnh/Audio bắt buộc phải có link media."
                        );
                    }
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

                // Apply update
                passage.Title = newTitle;
                passage.Content = newContent;
                passage.ImageUrl = newImageUrl;
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
