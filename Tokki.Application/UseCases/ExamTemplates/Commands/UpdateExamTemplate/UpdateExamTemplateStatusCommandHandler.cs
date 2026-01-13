using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.UpdateExamTemplateStatus
{
    public class UpdateExamTemplateStatusCommandHandler : IRequestHandler<UpdateExamTemplateStatusCommand, OperationResult<bool>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;

        public UpdateExamTemplateStatusCommandHandler(IExamTemplateRepository examTemplateRepository)
        {
            _examTemplateRepository = examTemplateRepository;
        }

        public async Task<OperationResult<bool>> Handle(UpdateExamTemplateStatusCommand request, CancellationToken cancellationToken)
        {
            var examTemplate = await _examTemplateRepository.GetByIdAsync(request.ExamTemplateId, cancellationToken);

            if (examTemplate == null)
            {
                return OperationResult<bool>.Failure("Không tìm thấy đề thi mẫu.");
            }

            if (examTemplate.Status == request.Status)
            {
                return OperationResult<bool>.Success(true);
            }         

            examTemplate.Status = request.Status;

            await _examTemplateRepository.UpdateAsync(examTemplate);
            await _examTemplateRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}