using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Exam.Queries.GetTemplateSkills
{
    public class GetTemplateSkillsQuery : IRequest<OperationResult<List<string>>>
    {
        public string TemplateId { get; set; } = string.Empty;
    }
}
