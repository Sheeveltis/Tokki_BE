using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.VocabSpacedRepetition.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.VocabSpacedRepetition.Queries.GetDueReviews
{
    public class GetDueReviewsQueryHandler : IRequestHandler<GetDueReviewsQuery, OperationResult<List<ReviewItemDTO>>>
    {
        private readonly IUserVocabProgressRepository _userVocabProgressRepository;

        public GetDueReviewsQueryHandler(IUserVocabProgressRepository userVocabProgressRepository)
        {
            _userVocabProgressRepository = userVocabProgressRepository;
        }

        public async Task<OperationResult<List<ReviewItemDTO>>> Handle(GetDueReviewsQuery request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow.AddHours(7);

            var result = await _userVocabProgressRepository.GetDueReviewsAsync(request.UserId, now, request.Limit, cancellationToken);

            return OperationResult<List<ReviewItemDTO>>.Success(result);
        }
    }
}
