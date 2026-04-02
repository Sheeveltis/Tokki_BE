using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.UserExam.DTOs;

namespace Tokki.Application.UseCases.UserExam.Queries.GetPracticeQuestions
{
    public class GetPracticeQuestionsQuery : IRequest<OperationResult<List<QuestionResultGroupDto>>>
    {
        public string QuestionTypeId { get; set; } = string.Empty;
        public int Quantity { get; set; } = 10; 
    }
}
