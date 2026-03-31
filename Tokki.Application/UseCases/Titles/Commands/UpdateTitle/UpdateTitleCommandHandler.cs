using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

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
                return OperationResult<Title>.Failure(new List<Error>(), 404, "Không tìm thấy danh hiệu");
            }

            if (title.Name != request.Name)
            {
                var duplicateCheck = await _titleRepository.GetTitleByNameAsync(request.Name);
                if (duplicateCheck != null)
                {
                    return OperationResult<Title>.Failure(new List<Error>(), 400, "Tên danh hiệu đã bị trùng với danh hiệu khác.");
                }
            }

            title.Name = request.Name;
            title.Description = request.Description;
            title.RequirementType = request.RequirementType;
            title.RequirementQuantity = request.RequirementQuantity;
            title.ColorHex = request.ColorHex;
            title.IconUrl = request.IconUrl;
            title.IsSystemGiven = request.IsSystemGiven;

            await _titleRepository.UpdateAsync(title);

            return OperationResult<Title>.Success(title, 200, "Cập nhật thành công");
        }
    }
}