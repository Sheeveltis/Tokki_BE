using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.UserExam.DTOs;

namespace Tokki.Application.UseCases.UserExam.Queries.GetUserExamResult
{
    public class GetUserExamResultQuery : IRequest<OperationResult<UserExamResultResponse>>
    {
        public string UserExamId { get; set; } = string.Empty;
    }
}
