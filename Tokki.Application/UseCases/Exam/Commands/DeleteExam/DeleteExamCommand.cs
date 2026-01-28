using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Exam.Commands.DeleteExam
{
    public class DeleteExamCommand : IRequest<OperationResult<string>>
    {
        [JsonIgnore]
        public string ExamId { get; set; } = string.Empty;
    }
}
