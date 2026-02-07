using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Exam.Commands.UpdateExamInfo
{
    public class UpdateExamInfoCommandHandler : IRequestHandler<UpdateExamInfoCommand, OperationResult<string>>
    {
        private readonly IExamRepository _examRepository;
        private readonly ILogger<UpdateExamInfoCommandHandler> _logger;

        public UpdateExamInfoCommandHandler(IExamRepository examRepository, ILogger<UpdateExamInfoCommandHandler> logger)
        {
            _examRepository = examRepository;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(UpdateExamInfoCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var exam = await _examRepository.GetByIdAsync(request.ExamId);
                if (exam == null)
                {
                    return OperationResult<string>.Failure(AppErrors.ExamNotFound, 404);
                }

                bool isDuplicate = await _examRepository.IsTitleExistsAsync(request.Title, request.ExamId, cancellationToken);

                if (isDuplicate)
                {
                    return OperationResult<string>.Failure($"Tên đề thi '{request.Title}' đã được sử dụng. Vui lòng chọn tên khác.", 400);
                }

                exam.Title = request.Title;
                exam.Duration = request.Duration;
                await _examRepository.UpdateAsync(exam);
                await _examRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(OperationMessages.UpdateSuccess("đề thi"));
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Failure(AppErrors.ServerError, 500);
            }
        }
    }
}
