using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Exam.Commands.AddQuestionToExam
{
    public class AddQuestionToExamCommand : IRequest<OperationResult<string>>
    {
        public string ExamId { get; set; } = string.Empty;
        public string QuestionBankId { get; set; } = string.Empty;
        public int QuestionNo { get; set; }
    }
}
