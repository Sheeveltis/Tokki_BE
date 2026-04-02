using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Exam.Queries.GetTemplateSkills
{
    public class GetTemplateSkillsQueryHandler : IRequestHandler<GetTemplateSkillsQuery, OperationResult<List<string>>>
    {
        private readonly ITemplatePartRepository _templatePartRepository;

        public GetTemplateSkillsQueryHandler(ITemplatePartRepository templatePartRepository)
        {
            _templatePartRepository = templatePartRepository;
        }

        public async Task<OperationResult<List<string>>> Handle(GetTemplateSkillsQuery request, CancellationToken cancellationToken)
        {
            var parts = await _templatePartRepository.GetByExamTemplateIdAsync(request.TemplateId, cancellationToken);

            if (parts == null || !parts.Any())
                return OperationResult<List<string>>.Failure("Template không có cấu trúc câu hỏi.", 404);

            var skills = parts.Select(p => p.Skill.ToString())
                              .Distinct()
                              .ToList();

            return OperationResult<List<string>>.Success(skills);
        }
    }
}
