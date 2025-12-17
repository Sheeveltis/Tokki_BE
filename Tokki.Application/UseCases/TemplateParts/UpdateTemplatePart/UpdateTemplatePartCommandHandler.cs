using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

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
            if (part == null)
                return OperationResult<string>.Failure(new List<Error> { AppErrors.TemplatePartNotFound }, 404, "Phần thi không tồn tại");

            if (request.QuestionFrom > request.QuestionTo || request.QuestionFrom <= 0)
                return OperationResult<string>.Failure(new List<Error> { AppErrors.TemplatePartInvalidRange }, 400, "Dải câu hỏi không hợp lệ");

            var otherParts = await _templatePartRepository.GetByExamTemplateIdAsync(part.ExamTemplateId, cancellationToken);
            foreach (var p in otherParts)
            {
                if (p.TemplatePartId == request.TemplatePartId) continue;

                if (request.QuestionFrom <= p.QuestionTo && request.QuestionTo >= p.QuestionFrom)
                {
                    return OperationResult<string>.Failure(new List<Error> { AppErrors.TemplatePartRangeOverlap }, 409, $"Dải câu hỏi trùng với phần '{p.PartTitle}'");
                }
            }

            try
            {
                part.PartTitle = request.PartTitle;
                part.Skill = request.Skill;
                part.QuestionFrom = request.QuestionFrom;
                part.QuestionTo = request.QuestionTo;
                part.Instruction = request.Instruction;
                part.DifficultyLevel = request.DifficultyLevel;
                part.QuestionTypeId = request.QuestionTypeId;
                part.ExampleType = request.ExampleType;
                part.ExampleData = request.ExampleData;

                await _templatePartRepository.UpdateAsync(part);
                await _templatePartRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(part.TemplatePartId, 200, "Cập nhật thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật Template Part");
                return OperationResult<string>.Failure(new List<Error> { AppErrors.ServerError }, 500, AppErrors.ServerError.Description);
            }
        }
    }
}