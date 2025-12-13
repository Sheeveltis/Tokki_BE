using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Exam.Commands.UpdateExam
{
    public class UpdateExamCommandHandler : IRequestHandler<UpdateExamCommand, OperationResult<string>>
    {
        private readonly IExamRepository _examRepository;
        private readonly ILogger<UpdateExamCommandHandler> _logger;

        public UpdateExamCommandHandler(
            IExamRepository examRepository,
            ILogger<UpdateExamCommandHandler> logger)
        {
            _examRepository = examRepository;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(UpdateExamCommand request, CancellationToken cancellationToken)
        {
            var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken);
            if (exam == null)
            {
                return OperationResult<string>.Failure(
                   new List<Tokki.Application.Common.Models.Error> { AppErrors.ExamNotFound },
                    404,
                    AppErrors.ExamNotFound.Description
                );
            }

            bool titleExists = await _examRepository.IsTitleExistsAsync(request.Title, request.ExamId);
            if (titleExists)
            {
                return OperationResult<string>.Failure(
                   new List<Tokki.Application.Common.Models.Error> { AppErrors.ExamTitleDuplicated },
                    409,
                    AppErrors.ExamTitleDuplicated.Description
                );
            }

            try
            {
                exam.Title = request.Title;
                exam.Type = request.Type;
                exam.Status = request.Status;

                await _examRepository.UpdateAsync(exam);
                await _examRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(
                    request.ExamId,
                    200,
                    "Cập nhật bài test thành công"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật bài test: {ExamId}", request.ExamId);
                return OperationResult<string>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
