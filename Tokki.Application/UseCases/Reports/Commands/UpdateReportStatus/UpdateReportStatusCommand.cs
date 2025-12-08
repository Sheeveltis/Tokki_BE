using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums; 
namespace Tokki.Application.UseCases.Reports.Commands.UpdateReportStatus
{
    public class UpdateReportStatusCommand : IRequest<OperationResult<bool>>
    {
        public string ReportId { get; set; } 

        public ReportStatus NewStatus { get; set; }

        public string? AdminReply { get; set; }
    }
}