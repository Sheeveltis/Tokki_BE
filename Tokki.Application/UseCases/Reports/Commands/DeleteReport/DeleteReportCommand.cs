using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Reports.Commands.DeleteReport
{
    public class DeleteReportCommand : IRequest<OperationResult<bool>>
    {
        public string ReportId { get; set; }
        public string UserId { get; set; } 
        public bool IsAdmin { get; set; } = false; 
    }
}