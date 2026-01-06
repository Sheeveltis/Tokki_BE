using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Passages.DTOs;

namespace Tokki.Application.UseCases.Passages.Queries.GetPassageById
{
    public class GetPassageByIdQueryHandler : IRequestHandler<GetPassageByIdQuery, OperationResult<PassageDto>>
    {
        private readonly IPassageRepository _passageRepository;

        public GetPassageByIdQueryHandler(IPassageRepository passageRepository)
        {
            _passageRepository = passageRepository;
        }

        public async Task<OperationResult<PassageDto>> Handle(GetPassageByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var id = request.PassageId.Trim();

                var p = await _passageRepository.GetByIdAsync(id, cancellationToken);
                if (p == null)
                {
                    return OperationResult<PassageDto>.Failure(
                        new List<Error> { AppErrors.PassageNotFound },
                        404,
                        AppErrors.PassageNotFound.Description
                    );
                }

                var dto = new PassageDto
                {
                    PassageId = p.PassageId,
                    Title = p.Title,
                    Content = p.Content,
                    ImageUrl = p.ImageUrl,
                    Status = p.Status,
                    MediaType = p.MediaType,
                    CreatedAt = p.CreatedAt
                };

                return OperationResult<PassageDto>.Success(dto, 200, "Thành công");
            }
            catch
            {
                return OperationResult<PassageDto>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
