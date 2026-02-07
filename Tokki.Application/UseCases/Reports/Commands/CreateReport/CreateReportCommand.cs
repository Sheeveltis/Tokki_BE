using MediatR;
using Tokki.Application.Common.Models; 

namespace Tokki.Application.UseCases.Reports.Commands.CreateReport
{
    public class CreateReportCommand : IRequest<OperationResult<string>>
    {
        public string UserId { get; set; } 
        public string Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? TargetUrl { get; set; }
        public string ReportType { get; set; } 
        public string? QuestionBankId { get; set; }
        public string? VocabularyId { get; set; }
    }
}