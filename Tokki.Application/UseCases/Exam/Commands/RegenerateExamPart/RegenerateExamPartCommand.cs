using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Exam.Commands.RegenerateExamPart
{
    public class RegenerateExamPartCommand : IRequest<OperationResult<bool>>
    {
        public string ExamId { get; set; } = default!;
        public string TemplatePartId { get; set; } = default!;
    }
}
