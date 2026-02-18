using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.UserExam.DTOs;

namespace Tokki.Application.UseCases.UserExam.Commands.CreateUserTakeExam
{
    public class CreateUserTakeExamCommand : IRequest<OperationResult<CreateUserTakeExamResponse>>
    {
        public string ExamId { get; set; } = string.Empty;
        public bool IsShuffle { get; set; } = false;
        [JsonIgnore]
        public string UserId { get; set; }
    }
}
