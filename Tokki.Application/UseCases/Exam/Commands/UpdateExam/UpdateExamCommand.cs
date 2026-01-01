using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.Commands.UpdateExam
{
    public class UpdateExamCommand : IRequest<OperationResult<string>>
    {
        public string ExamId { get; set; } = string.Empty;

        public string? Title { get; set; }
        public int? Duration { get; set; }
        public ExamType? Type { get; set; }
        public ExamStatus? Status { get; set; }

    }
}
