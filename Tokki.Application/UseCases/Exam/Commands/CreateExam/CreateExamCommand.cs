using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.Commands.CreateExam
{
    public class CreateExamCommand : IRequest<OperationResult<string>>
    {
        public string Title { get; set; } = string.Empty;
        public int Duration { get; set; } 
        public string ExamTemplateId { get; set; } = string.Empty;
        [JsonIgnore]
        public string CreatedBy { get; set; } = string.Empty;
    }
}
