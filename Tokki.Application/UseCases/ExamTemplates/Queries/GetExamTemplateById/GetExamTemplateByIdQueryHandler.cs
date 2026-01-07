using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.DTOs;
using Tokki.Application.UseCases.ExamTemplates.Queries.GetExamTemplateById;

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

        if (et == null) return OperationResult<ExamTemplateDto>.Failure(AppErrors.ExamTemplateNotFound);

        var dto = new ExamTemplateDto
        {
            ExamTemplateId = et.ExamTemplateId,
            Name = et.Name,
            Description = et.Description,
            CreatedAt = et.CreatedAt,
            Status = et.Status,
            Type = et.Type,
            TotalParts = et.TemplateParts.Count,
            TotalQuestions = et.TemplateParts.Sum(p => p.QuestionTo - p.QuestionFrom + 1),

            Parts = et.TemplateParts.Select(tp => new TemplatePartDto
            {
                TemplatePartId = tp.TemplatePartId,
                Skill = tp.Skill,
                QuestionFrom = tp.QuestionFrom,
                QuestionTo = tp.QuestionTo,
                PartTitle = tp.PartTitle,
                Instruction = tp.Instruction,
                Mark = tp.Mark,
                ExampleUrl = tp.ExampleUrl,
                QuestionTypeId = tp.QuestionTypeId ?? string.Empty,
                QuestionTypeName = tp.QuestionType != null ? tp.QuestionType.Name : string.Empty
            }).ToList()
        };

        return OperationResult<ExamTemplateDto>.Success(dto);
    }
}