using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.TemplateParts.Commands.UpdateTemplatePart
{
    public class UpdateTemplatePartCommandHandler : IRequestHandler<UpdateTemplatePartCommand, OperationResult<string>>
    {
        private readonly ITemplatePartRepository _templatePartRepository;
        private readonly ILogger<UpdateTemplatePartCommandHandler> _logger;

        public UpdateTemplatePartCommandHandler(ITemplatePartRepository templatePartRepository, ILogger<UpdateTemplatePartCommandHandler> logger)
        {
            _templatePartRepository = templatePartRepository;
            _logger = logger;
        }
        public async Task<OperationResult<string>> Handle(UpdateTemplatePartCommand request, CancellationToken cancellationToken)
        {
            var part = await _templatePartRepository.GetByIdAsync(request.TemplatePartId, cancellationToken);
            if (part == null) return OperationResult<string>.Failure(AppErrors.TemplatePartNotFound);

            if (request.QuestionFrom > request.QuestionTo || request.QuestionFrom <= 0)
                return OperationResult<string>.Failure(AppErrors.TemplatePartInvalidRange);

            bool isOverlap = await _templatePartRepository.IsQuestionRangeOverlapAsync(
                part.ExamTemplateId, request.QuestionFrom, request.QuestionTo, part.TemplatePartId);

            if (isOverlap) return OperationResult<string>.Failure(AppErrors.TemplatePartRangeOverlap);

            try
            {
                part.PartTitle = request.PartTitle;
                part.Skill = request.Skill;
                part.QuestionFrom = request.QuestionFrom;
                part.QuestionTo = request.QuestionTo;
                part.Instruction = request.Instruction;
                part.Mark = request.Mark;
                part.QuestionTypeId = request.QuestionTypeId;
                part.ExampleUrl = request.ExampleUrl;

                await _templatePartRepository.UpdateAsync(part);
                await _templatePartRepository.SaveChangesAsync(cancellationToken);
                return OperationResult<string>.Success(part.TemplatePartId, 200, "Cập nhật thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật Template Part");
                return OperationResult<string>.Failure(AppErrors.ServerError);
            }
        }
    }
}