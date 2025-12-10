using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Titles.Commands.DeleteTitle;
using Tokki.Domain.Enums; 

public class DeleteTitleCommandHandler : IRequestHandler<DeleteTitleCommand, OperationResult<string>>
{
    private readonly ITitleRepository _titleRepository;

    public DeleteTitleCommandHandler(ITitleRepository titleRepository)
    {
        _titleRepository = titleRepository;
    }

    public async Task<OperationResult<string>> Handle(DeleteTitleCommand request, CancellationToken cancellationToken)
    {
        var title = await _titleRepository.GetTitleByIdAsync(request.Id);

        if (title == null)
        {
            return OperationResult<string>.Failure(new List<Error>(), 404, "Không tìm thấy danh hiệu.");
        }

        title.Status = TitleStatus.Inactive;

        await _titleRepository.UpdateAsync(title);

        return OperationResult<string>.Success("Đã ẩn danh hiệu", 200, "Danh hiệu đã được chuyển sang trạng thái ẩn.");
    }
}