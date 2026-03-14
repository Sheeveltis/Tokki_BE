using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Roadmap.DTOs;

namespace Tokki.Application.UseCases.Roadmap.Queries.GetVirtualQuiz
{
    public class GetVirtualQuizQuery : IRequest<OperationResult<List<VirtualQuizQuestionViewModel>>>
    {
        public string QuestionTypeId { get; set; }
        public int Count { get; set; }

        public GetVirtualQuizQuery(string questionTypeId, int count = 10)
        {
            QuestionTypeId = questionTypeId;
            Count = count;
        }
    }
}