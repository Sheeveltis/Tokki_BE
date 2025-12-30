using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailTemplate
{
    public class DeleteEmailAutoTemplateCommandHandler : IRequestHandler<DeleteEmailAutoTemplateCommand, OperationResult<string>>
    {
        private readonly IEmailTemplateRepository _repository;

        public DeleteEmailAutoTemplateCommandHandler(IEmailTemplateRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<string>> Handle(DeleteEmailAutoTemplateCommand request, CancellationToken cancellationToken)
        {
            var template = await _repository.GetByIdAsync(request.TemplateId);
            if (template == null)
            {
                return OperationResult<string>.Failure(new List<Error>
                {
                    AppErrors.EmailTemplateNotFound
                });
            }

            // Nếu đã Deleted thì coi như OK (idempotent)
            if (template.Status == EmailTemplateStatus.Deleted)
            {
                return OperationResult<string>.Success(template.TemplateId, 200, "Template đã ở trạng thái Deleted.");
            }

            template.Status = EmailTemplateStatus.Deleted;
            template.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _repository.UpdateAsync(template);
            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(template.TemplateId, 200, "Xóa template thành công!");
        }
    }
}
