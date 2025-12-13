using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionBanks.DTOs;

namespace Tokki.Application.UseCases.QuestionBanks.Queries.GetQuestionBanks
{
    public class GetQuestionBanksQueryHandler : IRequestHandler<GetQuestionBanksQuery, OperationResult<PagedResult<QuestionBankDto>>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;

        public GetQuestionBanksQueryHandler(IQuestionBankRepository questionBankRepository)
        {
            _questionBankRepository = questionBankRepository;
        }

        public async Task<OperationResult<PagedResult<QuestionBankDto>>> Handle(
            GetQuestionBanksQuery request,
            CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _questionBankRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.Skill,
                request.DifficultyLevel,
                request.QuestionTypeId,
                request.PassageId,
                request.IsActive,
                cancellationToken
            );

            var dtos = items.Select(q => new QuestionBankDto
            {
                QuestionBankId = q.QuestionBankId,
                PassageId = q.PassageId,
                PassageTitle = q.Passage?.Title,
                QuestionTypeId = q.QuestionTypeId,
                QuestionTypeName = q.QuestionType?.Name,
                Skill = q.Skill,
                Content = q.Content,
                MediaUrl = q.MediaUrl,
                Explanation = q.Explanation,
                DifficultyLevel = q.DifficultyLevel,
                IsActive = q.IsActive,
                Options = q.QuestionOptions.Select(o => new QuestionOptionDto
                {
                    OptionId = o.OptionId,
                    KeyOption = o.KeyOption,
                    Content = o.Content,
                    ImageUrl = o.ImageUrl,
                    IsCorrect = o.IsCorrect
                }).OrderBy(o => o.KeyOption).ToList()
            }).ToList();

            var pagedResult = PagedResult<QuestionBankDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<QuestionBankDto>>.Success(
                pagedResult,
                200,
                $"Tìm thấy {totalCount} câu hỏi."
            );
        }
    }
}
