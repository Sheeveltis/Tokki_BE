using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.QuestionTypes.Commands.DeleteQuestionType
{
    public class DeleteQuestionTypeCommand : IRequest<OperationResult<Unit>>
    {
        public string Id { get; set; } = string.Empty;

        public DeleteQuestionTypeCommand(string id)
        {
            Id = id;
        }
    }
}