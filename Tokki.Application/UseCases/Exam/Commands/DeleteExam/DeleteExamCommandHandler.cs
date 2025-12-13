using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Exam.Commands.DeleteExam
{
    public class DeleteExamCommandHandler : IRequestHandler<DeleteExamCommand, OperationResult<bool>>
    {
        private readonly IExamRepository _examRepository;
        private readonly ILogger<DeleteExamCommandHandler> _logger;

        public DeleteExamCommandHandler(
            IExamRepository examRepository,
            ILogger<DeleteExamCommandHandler> logger)
        {
            _examRepository = examRepository;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(DeleteExamCommand request, CancellationToken cancellationToken)
        {
            var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken);
            if (exam == null)
            {
                return OperationResult<bool>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { AppErrors.ExamNotFound },
                    404,
                    AppErrors.ExamNotFound.Description
                );
            }

            try
            {
                await _examRepository.DeleteAsync(exam);
                await _examRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<bool>.Success(
                    true,
                    200,
                    "Xóa bài test thành công"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa bài test: {ExamId}", request.ExamId);
                return OperationResult<bool>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
