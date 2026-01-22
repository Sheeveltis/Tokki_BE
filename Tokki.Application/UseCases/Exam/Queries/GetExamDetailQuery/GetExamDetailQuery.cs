using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Exam.DTOs;

namespace Tokki.Application.UseCases.Exam.Queries.GetExamDetailQuery
{
    public class GetExamDetailQuery : IRequest<OperationResult<ExamDetailDto>>
    {
        public string ExamId { get; set; } = string.Empty;
    }
}
