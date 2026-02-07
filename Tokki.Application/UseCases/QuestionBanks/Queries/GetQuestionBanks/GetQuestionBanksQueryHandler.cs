using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionBanks.DTOs;

namespace Tokki.Application.UseCases.QuestionBanks.Queries.GetQuestionBanks
{
    public class GetQuestionBanksQueryHandler
        : IRequestHandler<GetQuestionBanksQuery, OperationResult<PagedResult<QuestionBankDto>>>
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
            // CALL REPO đúng signature hiện có (KHÔNG truyền CreateBy/ApprovedBy)
            var (items, totalCount) = await _questionBankRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.QuestionTypeId,
                request.PassageId,
                request.Status,
                cancellationToken
            );

            // OPTIONAL FILTER ở handler (lọc sau paging)
            var filtered = items.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.CreateBy))
            {
                var createBy = request.CreateBy.Trim();
                filtered = filtered.Where(q => !string.IsNullOrWhiteSpace(q.CreateBy) && q.CreateBy.Trim() == createBy);
            }

            if (!string.IsNullOrWhiteSpace(request.ApprovedBy))
            {
                var approvedBy = request.ApprovedBy.Trim();
                filtered = filtered.Where(q => !string.IsNullOrWhiteSpace(q.ApprovedBy) && q.ApprovedBy.Trim() == approvedBy);
            }

            var filteredList = filtered.ToList();

            var dtos = filteredList.Select(q => new QuestionBankDto
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

                // audit fields
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

            // IMPORTANT:
            // totalCount từ repo KHÔNG phản ánh filter CreateBy/ApprovedBy (vì filter diễn ra sau paging).
            // Để tránh hiểu nhầm, ta dùng count của list hiện tại làm totalCount "hiển thị" khi có filter audit.
            var hasAuditFilter = !string.IsNullOrWhiteSpace(request.CreateBy) || !string.IsNullOrWhiteSpace(request.ApprovedBy);
            var finalTotalCount = hasAuditFilter ? dtos.Count : totalCount;

            var pagedResult = PagedResult<QuestionBankDto>.Create(
                dtos,
                finalTotalCount,
                request.PageNumber,
                request.PageSize
            );

            var message = hasAuditFilter
                ? $"Tìm thấy {dtos.Count} câu hỏi trong trang hiện tại (lọc theo CreateBy/ApprovedBy thực hiện sau phân trang)."
                : $"Tìm thấy {totalCount} câu hỏi.";

            return OperationResult<PagedResult<QuestionBankDto>>.Success(
                pagedResult,
                200,
                message
            );
        }
    }
}
