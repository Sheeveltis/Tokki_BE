using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Alphabet.DTOs;

namespace Tokki.Application.UseCases.Alphabet.Queries
{
    public class GetAlphabetDetailQueryHandler : IRequestHandler<GetAlphabetDetailQuery, OperationResult<AlphabetDetailDto>>
    {
        private readonly IAlphabetRepository _alphabetRepo;

        public GetAlphabetDetailQueryHandler(IAlphabetRepository alphabetRepo)
        {
            _alphabetRepo = alphabetRepo;
        }

        public async Task<OperationResult<AlphabetDetailDto>> Handle(GetAlphabetDetailQuery request, CancellationToken cancellationToken)
        {
            var entity = await _alphabetRepo.GetByIdAsync(request.Id);

            if (entity == null)
            {
                return OperationResult<AlphabetDetailDto>.Failure(new Error("NOT_FOUND", "Không tìm thấy dữ liệu Alphabet."));
            }

            var result = new AlphabetDetailDto
            {
                Id = entity.Id,
                Letter = entity.Letter,
                Meaning = entity.Meaning,
                Pronunciation = entity.Pronunciation,
                Type = (int)entity.Type,
                TotalStrokes = entity.TotalStrokes,
                AudioUrl = entity.AudioUrl,
                DisplayDataJson = entity.DisplayDataJson,
                ValidationDataJson = entity.ValidationDataJson,
                SortOrder = entity.SortOrder,
                IsActive = entity.IsActive
            };

            return OperationResult<AlphabetDetailDto>.Success(result);
        }
    }
}
