using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Queries.GetByQuestionTypeId
{
    public class GetQuestionBanksByQuestionTypeIdQueryHandler
        : IRequestHandler<GetQuestionBanksByQuestionTypeIdQuery, OperationResult<List<QuestionBankByQuestionTypeDto>>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;

        public GetQuestionBanksByQuestionTypeIdQueryHandler(IQuestionBankRepository questionBankRepository)
        {
            _questionBankRepository = questionBankRepository;
        }

        public async Task<OperationResult<List<QuestionBankByQuestionTypeDto>>> Handle(
            GetQuestionBanksByQuestionTypeIdQuery request,
            CancellationToken cancellationToken)
        {
            var items = await _questionBankRepository.GetByQuestionTypeIdAsync(
                request.QuestionTypeId,
                request.Status,
                cancellationToken
            );

            var dtos = items.Select(q => new QuestionBankByQuestionTypeDto
            {
                QuestionBankId = q.QuestionBankId,
                PassageId = q.PassageId,
                PassageTitle = q.Passage?.Title,
                QuestionTypeId = q.QuestionTypeId,
                QuestionTypeName = q.QuestionType?.Name,
                Content = q.Content,
                MediaUrl = q.MediaUrl,
                Explanation = q.Explanation,
                Status = q.Status,
                Options = q.QuestionOptions
                    .Select(o => new QuestionOptionDto
                    {
                        OptionId = o.OptionId,
                        KeyOption = o.KeyOption,
                        Content = o.Content,
                        ImageUrl = o.ImageUrl,
                        IsCorrect = o.IsCorrect
                    })
                    .OrderBy(o => o.KeyOption)
                    .ToList()
            }).ToList();

            return OperationResult<List<QuestionBankByQuestionTypeDto>>.Success(
                dtos,
                200,
                $"Tìm thấy {dtos.Count} câu hỏi."
            );
        }
    }
}
