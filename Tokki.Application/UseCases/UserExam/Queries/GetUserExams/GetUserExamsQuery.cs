using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.UserExam.Queries.GetUserExams
{
    public class GetUserExamsQuery : IRequest<OperationResult<PagedResult<UserExamActionDto>>>
    {
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;

        public string? ExamId { get; set; }
        public UserExamStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
