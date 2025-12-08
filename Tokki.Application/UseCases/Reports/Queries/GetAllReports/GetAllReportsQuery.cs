using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Reports.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Reports.Queries.GetAllReports
{
    public class GetAllReportsQuery : IRequest<OperationResult<List<ReportNotificationDTO>>>
    {
        public ReportStatus? Status { get; set; }
    }
}