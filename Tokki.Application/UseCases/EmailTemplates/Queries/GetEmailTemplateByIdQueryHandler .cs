using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.EmailTemplates.Queries
{
    public class GetEmailTemplateByIdQueryHandler : IRequestHandler<GetEmailTemplateByIdQuery, OperationResult<EmailTemplate>>
    {
        private readonly IEmailTemplateRepository _repository;

        public GetEmailTemplateByIdQueryHandler(IEmailTemplateRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<EmailTemplate>> Handle(GetEmailTemplateByIdQuery request, CancellationToken cancellationToken)
        {
            var template = await _repository.GetByIdAsync(request.TemplateId);

            if (template == null)
            {
                return OperationResult<EmailTemplate>.Failure("Không tìm thấy template!", 404);
            }

            return OperationResult<EmailTemplate>.Success(template, 200);
        }
    }
}