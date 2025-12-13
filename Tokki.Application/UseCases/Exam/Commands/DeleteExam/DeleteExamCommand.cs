using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Exam.Commands.DeleteExam
{
    public class DeleteExamCommand : IRequest<OperationResult<bool>>
    {
        public string ExamId { get; set; } = string.Empty;
    }
}
