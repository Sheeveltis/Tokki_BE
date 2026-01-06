using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.DTOs;
using Tokki.Application.UseCases.ExamTemplates.Queries.GetExamTemplateById;

public class GetExamTemplateByIdQueryHandler : IRequestHandler<GetExamTemplateByIdQuery, OperationResult<ExamTemplateDto>>
{
    private readonly IExamTemplateRepository _examTemplateRepository;
    private readonly ITemplatePartRepository _templatePartRepository;
    public GetExamTemplateByIdQueryHandler(
        IExamTemplateRepository examTemplateRepository,
        ITemplatePartRepository templatePartRepository)
    {
        _examTemplateRepository = examTemplateRepository;
        _templatePartRepository = templatePartRepository;
    }

    public async Task<OperationResult<ExamTemplateDto>> Handle(GetExamTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        var et = await _examTemplateRepository.GetByIdAsync(request.ExamTemplateId, cancellationToken);
        if (et == null) return OperationResult<ExamTemplateDto>.Failure(AppErrors.ExamTemplateNotFound);

        var stats = await _templatePartRepository.GetStatsByTemplateIdAsync(request.ExamTemplateId);

        var dto = new ExamTemplateDto
        {
            ExamTemplateId = et.ExamTemplateId,
            Name = et.Name,
            Description = et.Description,
            CreatedAt = et.CreatedAt,
            Status = et.Status,
            Type = et.Type,
            TotalParts = stats.totalParts,
            TotalQuestions = stats.totalQuestions,            
        };

        return OperationResult<ExamTemplateDto>.Success(dto);
    }
}