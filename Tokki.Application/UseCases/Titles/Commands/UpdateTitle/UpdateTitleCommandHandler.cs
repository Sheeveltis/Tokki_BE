using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Titles.Commands.UpdateTitle
{
    public class UpdateTitleCommandHandler : IRequestHandler<UpdateTitleCommand, OperationResult<Title>>
    {
        private readonly ITitleRepository _titleRepository;

        public UpdateTitleCommandHandler(ITitleRepository titleRepository)
        {
            _titleRepository = titleRepository;
        }

        public async Task<OperationResult<Title>> Handle(UpdateTitleCommand request, CancellationToken cancellationToken)
        {
            var title = await _titleRepository.GetTitleByIdAsync(request.TitleId);
            if (title == null)
            {
                return OperationResult<Title>.Failure("Không tìm thấy danh hiệu.", 404);
            }

            if (!title.Name.Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                var duplicateCheck = await _titleRepository.GetTitleByNameAsync(request.Name.Trim(), TitleStatus.Active);
                if (duplicateCheck != null)
                {
                    return OperationResult<Title>.Failure($"Tên danh hiệu '{request.Name}' đã bị trùng với danh hiệu đang hoạt động khác.", 400);
                }
            }

            title.Name = request.Name.Trim();
            title.Description = request.Description?.Trim();
            title.RequirementType = request.RequirementType;
            title.RequirementQuantity = request.RequirementQuantity;
            title.ColorHex = request.ColorHex.Trim();
            title.IconUrl = request.IconUrl.Trim();

            await _titleRepository.UpdateAsync(title);

            return OperationResult<Title>.Success(title, 200, "Cập nhật danh hiệu thành công");
        }
    }
}