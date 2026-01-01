using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EmailTemplates.Queries.GetAllEmailTemplates
{
    public class GetAllEmailAutoTemplatesQueryHandler
        : IRequestHandler<GetAllEmailAutoTemplatesQuery, OperationResult<PagedResult<EmailTemplate>>>
    {
        private readonly IEmailTemplateRepository _repository;

        public GetAllEmailAutoTemplatesQueryHandler(IEmailTemplateRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<PagedResult<EmailTemplate>>> Handle(
            GetAllEmailAutoTemplatesQuery request,
            CancellationToken cancellationToken)
        {
            // Lấy toàn bộ templates qua GetPagedAsync (giống style mẫu Accounts)
            var (allTemplates, totalBeforeFilter) = await _repository.GetPagedAsync(1, int.MaxValue);

            var query = allTemplates.AsQueryable();

            // Mặc định: không trả Deleted nếu client không truyền Status
            if (!request.Status.HasValue)
            {
                query = query.Where(t => t.Status != EmailTemplateStatus.Deleted);
            }

            // Filter Status
            if (request.Status.HasValue)
            {
                query = query.Where(t => t.Status == request.Status.Value);
            }

            // Filter Type
            if (request.Type.HasValue)
            {
                query = query.Where(t => t.Type == request.Type.Value);
            }

            // Filter TargetGroup
            if (request.TargetGroup.HasValue)
            {
                query = query.Where(t => t.TargetGroup == request.TargetGroup.Value);
            }

            // Filter Value
            if (request.Value.HasValue)
            {
                query = query.Where(t => t.Value == request.Value.Value);
            }

            // Search TemplateName
            if (!string.IsNullOrWhiteSpace(request.SearchName))
            {
                var key = request.SearchName.Trim().ToLower();
                query = query.Where(t =>
                    t.TemplateName != null && t.TemplateName.ToLower().Contains(key));
            }

            // Search Subject
            if (!string.IsNullOrWhiteSpace(request.SearchSubject))
            {
                var key = request.SearchSubject.Trim().ToLower();
                query = query.Where(t =>
                    t.Subject != null && t.Subject.ToLower().Contains(key));
            }

            // Sort: mới nhất lên đầu
            query = query.OrderByDescending(t => t.CreateAt);

            var totalCount = query.Count();

            // Apply pagination
            var pagedItems = query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var pagedResult = PagedResult<EmailTemplate>.Create(
                pagedItems,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<EmailTemplate>>.Success(
                pagedResult,
                200,
                "Lấy danh sách email templates thành công"
            );
        }
    }
}
