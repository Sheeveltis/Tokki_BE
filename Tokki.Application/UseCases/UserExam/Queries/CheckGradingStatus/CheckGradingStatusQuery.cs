using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.UserExam.DTOs;

namespace Tokki.Application.UseCases.UserExam.Queries.CheckGradingStatus
{
    public class CheckGradingStatusQuery : IRequest<OperationResult<GradingStatusResponse>>
    {
        public string UserExamId { get; set; } = string.Empty;
    }
}
