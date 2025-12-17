using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.TemplateParts.CreateTemplatePart
{
    public class CreateTemplatePartCommandHandler : IRequestHandler<CreateTemplatePartCommand, OperationResult<string>>
    {
        private readonly ITemplatePartRepository _templatePartRepository;
        private readonly IExamTemplateRepository _examTemplateRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<CreateTemplatePartCommandHandler> _logger;

        public CreateTemplatePartCommandHandler(
            ITemplatePartRepository templatePartRepository,
            IExamTemplateRepository examTemplateRepository,
            IIdGeneratorService idGeneratorService,
            ILogger<CreateTemplatePartCommandHandler> logger)
        {
            _templatePartRepository = templatePartRepository;
            _examTemplateRepository = examTemplateRepository;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(CreateTemplatePartCommand request, CancellationToken cancellationToken)
        {
            var template = await _examTemplateRepository.GetByIdAsync(request.ExamTemplateId, cancellationToken);
            if (template == null)
                return OperationResult<string>.Failure(new List<Error> { AppErrors.ExamTemplateNotFound }, 404, "Mẫu đề không tồn tại");

            if (request.QuestionFrom > request.QuestionTo || request.QuestionFrom <= 0)
                return OperationResult<string>.Failure(new List<Error> { AppErrors.TemplatePartInvalidRange }, 400, "Dải câu hỏi không hợp lệ");

            bool isOverlap = await _templatePartRepository.IsQuestionRangeOverlapAsync(
                request.ExamTemplateId,
                request.QuestionFrom,
                request.QuestionTo,
                excludePartId: null
            );

            if (isOverlap)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.TemplatePartRangeOverlap },
                    409,
                    $"Dải câu hỏi ({request.QuestionFrom}-{request.QuestionTo}) bị trùng với phần khác trong đề thi."
                );
            }

            try
            {
                var newPart = new TemplatePart
                {
                    TemplatePartId = _idGeneratorService.GenerateCustom(10),
                    ExamTemplateId = request.ExamTemplateId,
                    PartTitle = request.PartTitle,
                    Skill = request.Skill, 
                    QuestionFrom = request.QuestionFrom,
                    QuestionTo = request.QuestionTo,
                    Instruction = request.Instruction,
                    DifficultyLevel = request.DifficultyLevel,
                    QuestionTypeId = request.QuestionTypeId,
                    ExampleType = request.ExampleType,
                    ExampleData = request.ExampleData
                };

                await _templatePartRepository.AddAsync(newPart);
                await _templatePartRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(newPart.TemplatePartId, 201, "Thêm phần thi thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi thêm Template Part");
                return OperationResult<string>.Failure(new List<Error> { AppErrors.ServerError }, 500, AppErrors.ServerError.Description);
            }
        }
    }
}