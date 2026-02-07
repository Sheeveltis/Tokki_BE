using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.Commands.UpdateExamStatus
{
    public class UpdateExamStatusCommand : IRequest<OperationResult<string>>
    {
        [JsonIgnore]
        public string ExamId { get; set; }
        public ExamStatus Status { get; set; } 
    }
}
