using MediatR;
using Microsoft.AspNetCore.Http;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Excel.Commands.ImportPronunciationRules
{
    public class ImportPronunciationRulesCommand : IRequest<OperationResult<ImportRulesResponse>>
    {
        public string UserId { get; set; } = string.Empty;
        public IFormFile File { get; set; } = null!;
    }

    public class ImportRulesResponse
    {
        public List<RulePreviewDTO> SuccessList { get; set; } = new();
        public List<RulePreviewDTO> FailureList { get; set; } = new();
    }

    public class RulePreviewDTO
    {
        public string RuleName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
