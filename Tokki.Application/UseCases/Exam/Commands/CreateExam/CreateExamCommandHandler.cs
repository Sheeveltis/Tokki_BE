using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.Commands.CreateExam
{
    public class CreateExamCommandHandler : IRequestHandler<CreateExamCommand, OperationResult<string>>
    {
        private readonly IExamRepository _examRepository;
        private readonly IExamTemplateRepository _examTemplateRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<CreateExamCommandHandler> _logger;

        public CreateExamCommandHandler(
            IExamRepository examRepository,
            IExamTemplateRepository examTemplateRepository,
            IIdGeneratorService idGeneratorService,
            ILogger<CreateExamCommandHandler> logger)
        {
            _examRepository = examRepository;
            _examTemplateRepository = examTemplateRepository;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(CreateExamCommand request, CancellationToken cancellationToken)
        {
            // Validate ExamTemplate exists
            var examTemplate = await _examTemplateRepository.GetByIdAsync(request.ExamTemplateId, cancellationToken);
            if (examTemplate == null)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ExamTemplateNotFound },
                    404,
                    AppErrors.ExamTemplateNotFound.Description
                );
            }

            // Validate title không trùng
            bool titleExists = await _examRepository.IsTitleExistsAsync(request.Title);
            if (titleExists)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ExamTitleDuplicated },
                    409,
                    AppErrors.ExamTitleDuplicated.Description
                );
            }

            try
            {
                string examId = _idGeneratorService.GenerateCustom(10);

                var exam = new Domain.Entities.Exam
                {
                    ExamId = examId,
                    ExamTemplateId = request.ExamTemplateId,
                    Title = request.Title,
                    Type = request.Type,
                    Status = ExamStatus.Draft,
                    CreatedAt = DateTime.UtcNow,
                    Duration = request.Duration,
                };

                await _examRepository.AddAsync(exam);
                await _examRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(
                    examId,
                    201,
                    "Tạo bài test thành công"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo bài test: {Title}", request.Title);
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
