using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.DTOs;

namespace Tokki.Application.UseCases.ExamTemplates.Queries.GetExamTemplateById
{
    public class GetExamTemplateByIdQueryHandler : IRequestHandler<GetExamTemplateByIdQuery, OperationResult<ExamTemplateDto>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;

        public GetExamTemplateByIdQueryHandler(IExamTemplateRepository examTemplateRepository)
        {
            _examTemplateRepository = examTemplateRepository;
        }

        public async Task<OperationResult<ExamTemplateDto>> Handle(
            GetExamTemplateByIdQuery request,
            CancellationToken cancellationToken)
        {
            var examTemplate = await _examTemplateRepository.GetByIdWithPartsAsync(request.ExamTemplateId, cancellationToken);

            if (examTemplate == null)
            {
                return OperationResult<ExamTemplateDto>.Failure(
                   new List<Tokki.Application.Common.Models.Error> { AppErrors.ExamTemplateNotFound },
                    404,
                    AppErrors.ExamTemplateNotFound.Description
                );
            }

            var dto = new ExamTemplateDto
            {
                ExamTemplateId = examTemplate.ExamTemplateId,
                Name = examTemplate.Name,
                Description = examTemplate.Description,
                CreatedAt = examTemplate.CreatedAt,
                Status = examTemplate.Status,
                TotalParts = examTemplate.TemplateParts.Count,
                TotalQuestions = examTemplate.TemplateParts.Any()
                    ? examTemplate.TemplateParts.Max(tp => tp.QuestionTo)
                    : 0,
                Parts = examTemplate.TemplateParts.Select(tp => new TemplatePartDto
                {
                    TemplatePartId = tp.TemplatePartId,
                    Skill = tp.Skill,
                    QuestionFrom = tp.QuestionFrom,
                    QuestionTo = tp.QuestionTo,
                    PartTitle = tp.PartTitle,
                    Instruction = tp.Instruction,
                    ExampleType = tp.ExampleType,
                    ExampleData = tp.ExampleData
                }).OrderBy(tp => tp.QuestionFrom).ToList()
            };

            return OperationResult<ExamTemplateDto>.Success(
                dto,
                200,
                "Lấy thông tin mẫu đề thi thành công"
            );
        }
    }
    }
