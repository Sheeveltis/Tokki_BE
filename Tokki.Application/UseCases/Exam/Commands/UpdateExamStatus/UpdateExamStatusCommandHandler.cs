using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.Commands.UpdateExamStatus
{
    public class UpdateExamStatusCommandHandler : IRequestHandler<UpdateExamStatusCommand, OperationResult<string>>
    {
        private readonly IExamRepository _examRepository;

        public UpdateExamStatusCommandHandler(IExamRepository examRepository)
        {
            _examRepository = examRepository;
        }

        public async Task<OperationResult<string>> Handle(UpdateExamStatusCommand request, CancellationToken cancellationToken)
        {
            var exam = await _examRepository.GetByIdAsync(request.ExamId);
            if (exam == null)
            {
                return OperationResult<string>.Failure(AppErrors.ExamNotFound, 404);
            }

            if (!Enum.IsDefined(typeof(ExamStatus), request.Status))
            {
                return OperationResult<string>.Failure("Trạng thái không hợp lệ.", 400);
            }

            exam.Status = request.Status;

            await _examRepository.UpdateAsync(exam);
            await _examRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(OperationMessages.UpdateSuccess("trạng thái đề thi"));
        }
    }
}
