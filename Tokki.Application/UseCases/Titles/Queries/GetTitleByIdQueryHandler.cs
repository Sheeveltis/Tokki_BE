using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Titles.Queries.GetTitleById
{
    public class GetTitleByIdQueryHandler : IRequestHandler<GetTitleByIdQuery, OperationResult<Title>>
    {
        private readonly ITitleRepository _titleRepository;

        public GetTitleByIdQueryHandler(ITitleRepository titleRepository)
        {
            _titleRepository = titleRepository;
        }

        public async Task<OperationResult<Title>> Handle(GetTitleByIdQuery request, CancellationToken cancellationToken)
        {
            var title = await _titleRepository.GetTitleByIdAsync(request.Id);

            if (title == null)
            {
                return OperationResult<Title>.Failure(new List<Error>(), 404, "Không tìm thấy danh hiệu.");
            }

            return OperationResult<Title>.Success(title, 200);
        }
    }
}