using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.ExamTemplates.DTOs;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.AddTemplateParts
{
    public class AddTemplatePartsCommand : IRequest<OperationResult<bool>>
    {
        public string ExamTemplateId { get; set; } = string.Empty;
        public List<CreateTemplatePartDto> Parts { get; set; } = new();
    }
}