using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.ExamTemplates.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.UpdateExamTemplate
{
    public class UpdateExamTemplateCommand : IRequest<OperationResult<string>>
    {
        public string ExamTemplateId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ExamTemplateStatus Status { get; set; }
        public List<UpdateTemplatePartDto> Parts { get; set; } = new();
    }

}
