using global::Tokki.Application.Common.Models;
using global::Tokki.Application.IRepositories;
using global::Tokki.Domain.Entities;
using MediatR;
using Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailTemplate;

namespace Tokki.Application.UseCases.EmailTemplates.Commands
{
   
    namespace Tokki.Application.UseCases.EmailTemplates.Commands
    {
        public class CreateEmailTemplateCommandHandler : IRequestHandler<CreateEmailTemplateCommand, OperationResult<int>>
        {
            private readonly IEmailTemplateRepository _repository;

            public CreateEmailTemplateCommandHandler(IEmailTemplateRepository repository)
            {
                _repository = repository;
            }

            public async Task<OperationResult<int>> Handle(CreateEmailTemplateCommand request, CancellationToken cancellationToken)
            {
                // Kiểm tra TemplateKey đã tồn tại chưa
                var existing = await _repository.GetByKeyAsync(request.TemplateKey);
                if (existing != null)
                {
                    return OperationResult<int>.Failure($"TemplateKey '{request.TemplateKey}' đã tồn tại!", 400);
                }

                var template = new EmailTemplate
                {
                    TemplateKey = request.TemplateKey,
                    Subject = request.Subject,
                    Body = request.Body,
                    Description = request.Description,
                    UpdatedAt = DateTime.UtcNow.AddHours(7)
                };

                await _repository.AddAsync(template);
                await _repository.SaveChangesAsync(cancellationToken);

                return OperationResult<int>.Success(template.TemplateId, 201, "Tạo template thành công!");
            }
        }
    }

}
