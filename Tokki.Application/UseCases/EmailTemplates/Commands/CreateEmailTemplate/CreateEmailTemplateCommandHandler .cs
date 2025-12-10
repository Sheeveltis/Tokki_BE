using global::Tokki.Application.Common.Models;
using global::Tokki.Application.IRepositories;
using global::Tokki.Domain.Entities;
using MediatR;
using Tokki.Application.IServices; 
namespace Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailTemplate
{
    public class CreateEmailTemplateCommandHandler : IRequestHandler<CreateEmailTemplateCommand, OperationResult<string>> // ✅ Đổi int → string
    {
        private readonly IEmailTemplateRepository _repository;
        private readonly IIdGeneratorService _idGenerator;

        public CreateEmailTemplateCommandHandler(
            IEmailTemplateRepository repository,
            IIdGeneratorService idGenerator) 
        {
            _repository = repository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<string>> Handle(CreateEmailTemplateCommand request, CancellationToken cancellationToken)
        {
            // Kiểm tra TemplateKey đã tồn tại chưa
            var existing = await _repository.GetByKeyAsync(request.TemplateKey);
            if (existing != null)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.EmailTemplateKeyDuplicated });
            }

            var template = new EmailTemplate
            {
                TemplateId = _idGenerator.Generate(15), 
                TemplateKey = request.TemplateKey,
                Subject = request.Subject,
                Body = request.Body,
                Description = request.Description,
                UpdatedAt = DateTime.UtcNow.AddHours(7)
            };

            await _repository.AddAsync(template);
            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(template.TemplateId, 201, "Tạo template thành công!");
        }
    }
}