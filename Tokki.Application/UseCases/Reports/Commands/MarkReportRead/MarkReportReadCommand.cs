using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Reports.Commands.MarkReportRead
{
    public class MarkReportReadCommand : IRequest<OperationResult<bool>>
    {
        public string ReportId { get; set; }
        public string UserId { get; set; } 
    }
}