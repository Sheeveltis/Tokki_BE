using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EmailTemplates.Queries.GetEmailAutoTemplateById
{
    public class GetEmailAutoTemplateByIdQueryHandler
        : IRequestHandler<GetEmailAutoTemplateByIdQuery, OperationResult<EmailTemplate>>
    {
        private readonly IEmailTemplateRepository _repository;

        public GetEmailAutoTemplateByIdQueryHandler(IEmailTemplateRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<EmailTemplate>> Handle(GetEmailAutoTemplateByIdQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.TemplateId))
            {
                return OperationResult<EmailTemplate>.Failure(new List<Error>
                {
                    AppErrors.EmailTemplateNotFound
                });
            }

            var template = await _repository.GetByIdAsync(request.TemplateId);

            // Không tồn tại hoặc đã soft-delete thì coi như không tìm thấy
            if (template == null || template.Status == EmailTemplateStatus.Deleted)
            {
                return OperationResult<EmailTemplate>.Failure(new List<Error>
                {
                    AppErrors.EmailTemplateNotFound
                });
            }

            return OperationResult<EmailTemplate>.Success(template, 200, "Lấy template thành công");
        }
    }
}
