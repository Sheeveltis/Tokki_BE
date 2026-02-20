using MediatR;
using System.Collections.Generic;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Exam.Commands.SubmitExam
{
    public class SubmitExamCommand : IRequest<OperationResult<int>> 
    {
        public string ExamId { get; set; }
        public string UserId { get; set; }
        public List<UserAnswerDto> Answers { get; set; } = new List<UserAnswerDto>();
    }

    public class UserAnswerDto
    {
        public string QuestionId { get; set; }
        public string SelectedOptionId { get; set; }
    }
}