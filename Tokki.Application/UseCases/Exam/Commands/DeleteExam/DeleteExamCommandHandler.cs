using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.Commands.DeleteExam
{
    public class DeleteExamCommandHandler : IRequestHandler<DeleteExamCommand, OperationResult<string>>
    {
        private readonly IExamRepository _examRepository;

        public DeleteExamCommandHandler(IExamRepository examRepository)
        {
            _examRepository = examRepository;
        }

        public async Task<OperationResult<string>> Handle(DeleteExamCommand request, CancellationToken cancellationToken)
        {
            var exam = await _examRepository.GetByIdAsync(request.ExamId);
            if (exam == null)
            {
                return OperationResult<string>.Failure(AppErrors.ExamNotFound, 404);
            }

            exam.Status = ExamStatus.Deleted;

            await _examRepository.UpdateAsync(exam);
            await _examRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(OperationMessages.DeleteSuccess("đề thi"));
        }
    }
}
