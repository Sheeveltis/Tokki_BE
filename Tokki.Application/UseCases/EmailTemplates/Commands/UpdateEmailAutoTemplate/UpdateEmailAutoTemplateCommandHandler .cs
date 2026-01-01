using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailTemplate
{
    public class UpdateEmailAutoTemplateCommandHandler
        : IRequestHandler<UpdateEmailAutoTemplateCommand, OperationResult<string>>
    {
        private readonly IEmailTemplateRepository _repository;

        public UpdateEmailAutoTemplateCommandHandler(IEmailTemplateRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<string>> Handle(UpdateEmailAutoTemplateCommand request, CancellationToken cancellationToken)
        {
            var template = await _repository.GetByIdAsync(request.TemplateId);
            if (template == null)
            {
                return OperationResult<string>.Failure(new List<Error>
                {
                    AppErrors.EmailTemplateNotFound
                });
            }

            // Tính "giá trị sau cập nhật" để check unique (chỉ khi có thay đổi)
            var newTemplateName = !string.IsNullOrWhiteSpace(request.TemplateName)
                ? request.TemplateName.Trim()
                : template.TemplateName;

            var newType = request.Type ?? template.Type;
            var newValue = request.Value ?? template.Value;
            var newTargetGroup = request.TargetGroup ?? template.TargetGroup;

            var anyChanged = false;

            // 1) Check trùng TemplateName nếu có đổi
            if (newTemplateName != template.TemplateName)
            {
                var existingByName = await _repository.GetByNameAsync(newTemplateName);
                if (existingByName != null
                    && existingByName.TemplateId != template.TemplateId
                    && existingByName.Status != EmailTemplateStatus.Deleted)
                {
                    return OperationResult<string>.Failure(new List<Error>
                    {
                        AppErrors.EmailTemplateKeyDuplicated
                    });
                }

                template.TemplateName = newTemplateName;
                anyChanged = true;
            }

            // 2) Check trùng config nếu có đổi Type/Value/TargetGroup
            var configChanged = (newType != template.Type) || (newValue != template.Value) || (newTargetGroup != template.TargetGroup);
            if (configChanged)
            {
                var existingByConfig = await _repository.GetByTypeValueTargetAsync(newType, newValue, newTargetGroup);
                if (existingByConfig != null
                    && existingByConfig.TemplateId != template.TemplateId
                    && existingByConfig.Status != EmailTemplateStatus.Deleted)
                {
                    return OperationResult<string>.Failure(new List<Error>
                    {
                        AppErrors.EmailTemplateKeyDuplicated
                    });
                }

                template.Type = newType;
                template.Value = newValue;
                template.TargetGroup = newTargetGroup;
                anyChanged = true;
            }

            // 3) Update Status nếu có truyền
            if (request.Status.HasValue && request.Status.Value != template.Status)
            {
                template.Status = request.Status.Value;
                anyChanged = true;
            }

            // 4) Update Subject/Body/Description nếu không rỗng
            if (!string.IsNullOrWhiteSpace(request.Subject) && request.Subject != template.Subject)
            {
                template.Subject = request.Subject;
                anyChanged = true;
            }

            if (!string.IsNullOrWhiteSpace(request.Body) && request.Body != template.Body)
            {
                template.Body = request.Body;
                anyChanged = true;
            }

            if (!string.IsNullOrWhiteSpace(request.Description) && request.Description != template.Description)
            {
                template.Description = request.Description;
                anyChanged = true;
            }

            // Không có gì thay đổi thì trả về luôn
            if (!anyChanged)
            {
                return OperationResult<string>.Success(template.TemplateId, 200, "Không có dữ liệu hợp lệ để cập nhật!");
            }

            template.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _repository.UpdateAsync(template);
            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(template.TemplateId, 200, "Cập nhật template thành công!");
        }
    }
}
