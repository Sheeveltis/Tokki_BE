
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.ExamTemplates.DTOs;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.CreateExamTemplate
{
    public class CreateExamTemplateCommand : IRequest<OperationResult<string>>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<CreateTemplatePartDto> Parts { get; set; } = new();
    }
}
