using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.Queries.GetExamTemplateById
{
    public class GetExamTemplateByIdQueryHandler : IRequestHandler<GetExamTemplateByIdQuery, OperationResult<ExamTemplateDto>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;

        public GetExamTemplateByIdQueryHandler(IExamTemplateRepository examTemplateRepository)
        {
            _examTemplateRepository = examTemplateRepository;
        }

        public async Task<OperationResult<ExamTemplateDto>> Handle(GetExamTemplateByIdQuery request, CancellationToken cancellationToken)
        {
            var et = await _examTemplateRepository.GetByIdWithPartsAsync(request.ExamTemplateId, cancellationToken);

            if (et == null || et.Status == ExamTemplateStatus.Deleted)
            {
                return OperationResult<ExamTemplateDto>.Failure(AppErrors.ExamTemplateNotFound);
            }

            var dto = new ExamTemplateDto
            {
                ExamTemplateId = et.ExamTemplateId,
                Name = et.Name,
                Description = et.Description,
                Type = et.Type,
                CreatedAt = et.CreatedAt,
                Status = et.Status,
                TotalParts = et.TemplateParts.Count,
                TotalQuestions = et.TemplateParts.Sum(p => p.QuestionTo - p.QuestionFrom + 1),
                Parts = et.TemplateParts.OrderBy(p => p.QuestionFrom).Select(p => new TemplatePartDto
                {
                    TemplatePartId = p.TemplatePartId,
                    Skill = p.Skill,
                    QuestionFrom = p.QuestionFrom,
                    QuestionTo = p.QuestionTo,
                    PartTitle = p.PartTitle,
                    Instruction = p.Instruction,
                    Mark = p.Mark,
                    ExampleUrl = p.ExampleUrl,
                    QuestionTypeId = p.QuestionTypeId,
                    QuestionTypeName = p.QuestionType != null ? p.QuestionType.Name : "" 
                }).ToList()
            };

            return OperationResult<ExamTemplateDto>.Success(dto);
        }
    }
}