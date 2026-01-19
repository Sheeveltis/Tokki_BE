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
            var questionTypeId = request.QuestionTypeId?.Trim();
            if (string.IsNullOrWhiteSpace(questionTypeId))
            {
                return OperationResult<List<QuestionBankByQuestionTypeDto>>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "QuestionTypeId không hợp lệ."
                );
            }

            var items = await _questionBankRepository.GetByQuestionTypeIdAsync(
                questionTypeId,
                request.Status,
                cancellationToken
            );

            // NEW: filter audit ở handler (không cần sửa repo)
            IEnumerable<Tokki.Domain.Entities.QuestionBank> filtered = items;

            if (!string.IsNullOrWhiteSpace(request.CreateBy))
            {
                var cb = request.CreateBy.Trim();
                filtered = filtered.Where(q => !string.IsNullOrWhiteSpace(q.CreateBy) && q.CreateBy.Trim() == cb);
            }

            if (!string.IsNullOrWhiteSpace(request.ApprovedBy))
            {
                var ab = request.ApprovedBy.Trim();
                filtered = filtered.Where(q => !string.IsNullOrWhiteSpace(q.ApprovedBy) && q.ApprovedBy.Trim() == ab);
            }

            var dtos = filtered.Select(q => new QuestionBankByQuestionTypeDto
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

                // NEW: audit mapping
                CreateBy = q.CreateBy,
                CreatedAt = q.CreatedAt,
                ApprovedBy = q.ApprovedBy,
                ApprovedDate = q.ApprovedDate,

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
