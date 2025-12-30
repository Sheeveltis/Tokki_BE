using global::Tokki.Application.Common.Models;
using global::Tokki.Application.IRepositories;
using global::Tokki.Domain.Entities;
using MediatR;
using Tokki.Application.IServices;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailTemplate
{
    public class CreateEmailAutoTemplateCommandHandler
        : IRequestHandler<CreateEmailAutoTemplateCommand, OperationResult<string>>
    {
        private readonly IEmailTemplateRepository _repository;
        private readonly IIdGeneratorService _idGenerator;

        public CreateEmailAutoTemplateCommandHandler(
            IEmailTemplateRepository repository,
            IIdGeneratorService idGenerator)
        {
            _repository = repository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<string>> Handle(CreateEmailAutoTemplateCommand request, CancellationToken cancellationToken)
        {
            // Check trùng TemplateName
            var existingByName = await _repository.GetByNameAsync(request.TemplateName);
            if (existingByName != null && existingByName.Status != EmailTemplateStatus.Deleted)
            {
                return OperationResult<string>.Failure(new List<Error>
                {
                    AppErrors.EmailTemplateKeyDuplicated
                });
            }

            // Check trùng cấu hình (Type + Value + TargetGroup)
            var existingByLogic = await _repository.GetByTypeValueTargetAsync(request.Type, request.Value, request.TargetGroup);
            if (existingByLogic != null && existingByLogic.Status != EmailTemplateStatus.Deleted)
            {
                return OperationResult<string>.Failure(new List<Error>
                {
                    AppErrors.EmailTemplateKeyDuplicated
                });
            }

            var now = DateTime.UtcNow.AddHours(7);

            var template = new EmailTemplate
            {
                TemplateId = _idGenerator.Generate(15),
                TemplateName = request.TemplateName,
                Type = request.Type,
                Value = request.Value,
                TargetGroup = request.TargetGroup,
                Status = EmailTemplateStatus.Draft,

                Subject = request.Subject,
                Body = request.Body,
                Description = request.Description,

                CreateAt = now,
                UpdatedAt = now
            };

            await _repository.AddAsync(template);
            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(template.TemplateId, 201, "Tạo template thành công!");
        }
    }
}
