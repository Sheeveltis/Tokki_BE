using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.UserExam.Commands.MoveToNextSkill
{
    public class MoveToNextSkillCommand : IRequest<OperationResult<bool>>
    {
        public string UserExamId { get; set; } = string.Empty;
    }
}
