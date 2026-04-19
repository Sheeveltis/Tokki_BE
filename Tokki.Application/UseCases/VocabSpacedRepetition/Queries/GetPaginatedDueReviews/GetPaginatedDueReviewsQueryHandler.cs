using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.VocabSpacedRepetition.DTOs;

namespace Tokki.Application.UseCases.VocabSpacedRepetition.Queries.GetPaginatedDueReviews
{
    public class GetPaginatedDueReviewsQueryHandler : IRequestHandler<GetPaginatedDueReviewsQuery, OperationResult<PagedResult<ReviewItemDTO>>>
    {
        private readonly IUserVocabProgressRepository _userVocabProgressRepository;

        public GetPaginatedDueReviewsQueryHandler(IUserVocabProgressRepository userVocabProgressRepository)
        {
            _userVocabProgressRepository = userVocabProgressRepository;
        }

        public async Task<OperationResult<PagedResult<ReviewItemDTO>>> Handle(GetPaginatedDueReviewsQuery request, CancellationToken cancellationToken)
        {
            var now = DateTime.Now;

            var result = await _userVocabProgressRepository.GetPaginatedDueReviewsAsync(
                request.UserId, 
                now, 
                request.PageIndex, 
                request.PageSize, 
                cancellationToken);

            return OperationResult<PagedResult<ReviewItemDTO>>.Success(result);
        }
    }
}
