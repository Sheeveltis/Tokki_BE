using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.UpdateExamTemplate
{
    public class UpdateExamTemplateCommandHandler : IRequestHandler<UpdateExamTemplateCommand, OperationResult<bool>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;

        public UpdateExamTemplateCommandHandler(IExamTemplateRepository examTemplateRepository)
        {
            _examTemplateRepository = examTemplateRepository;
        }

        public async Task<OperationResult<bool>> Handle(UpdateExamTemplateCommand request, CancellationToken cancellationToken)
        {
            var examTemplate = await _examTemplateRepository.GetByIdAsync(request.ExamTemplateId, cancellationToken);

            if (examTemplate == null)
                return OperationResult<bool>.Failure("Không tìm thấy đề thi mẫu.");

            if (examTemplate.Status != ExamTemplateStatus.Draft)
                return OperationResult<bool>.Failure("Chỉ có thể cập nhật thông tin khi đề thi đang ở trạng thái Nháp.");

            if (await _examTemplateRepository.IsNameExistsAsync(request.Name, request.ExamTemplateId))
                return OperationResult<bool>.Failure("Tên đề thi đã tồn tại.");

            examTemplate.Name = request.Name;
            examTemplate.Description = request.Description;
            examTemplate.Type = request.Type;

            await _examTemplateRepository.UpdateAsync(examTemplate);
            await _examTemplateRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}