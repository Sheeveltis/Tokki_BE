using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.TemplateParts.Commands.CreateTemplatePart
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
            if (template == null) return OperationResult<string>.Failure(AppErrors.ExamTemplateNotFound);

            if (request.QuestionFrom > request.QuestionTo || request.QuestionFrom <= 0)
                return OperationResult<string>.Failure(AppErrors.TemplatePartInvalidRange);

            bool isOverlap = await _templatePartRepository.IsQuestionRangeOverlapAsync(
                request.ExamTemplateId, request.QuestionFrom, request.QuestionTo, null);

            if (isOverlap) return OperationResult<string>.Failure(AppErrors.TemplatePartRangeOverlap);

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
                    Mark = request.Mark,
                    QuestionTypeId = request.QuestionTypeId,
                    ExampleUrl = request.ExampleUrl
                };

                await _templatePartRepository.AddAsync(newPart);
                await _templatePartRepository.SaveChangesAsync(cancellationToken);
                return OperationResult<string>.Success(newPart.TemplatePartId, 201, "Thêm phần thi thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi thêm Template Part");
                return OperationResult<string>.Failure(AppErrors.ServerError);
            }
        }
    }
}