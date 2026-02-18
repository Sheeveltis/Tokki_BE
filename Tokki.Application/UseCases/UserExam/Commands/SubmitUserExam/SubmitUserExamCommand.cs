using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.UserExam.DTOs;

namespace Tokki.Application.UseCases.UserExam.Commands.SubmitUserExam
{
    public class SubmitUserExamCommand : IRequest<OperationResult<SubmitExamResponse>>
    {
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
        public string UserExamId { get; set; } = string.Empty;
    }
}
