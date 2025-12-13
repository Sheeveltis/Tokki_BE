using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.UpdateQuestionBank
{
    public class UpdateQuestionBankCommand : IRequest<OperationResult<string>>
    {
        public string QuestionBankId { get; set; } = string.Empty;
        public string? PassageId { get; set; }
        public string? QuestionTypeId { get; set; }
        public QuestionSkill Skill { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? MediaUrl { get; set; }
        public string? Explanation { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; }
        public bool IsActive { get; set; }
        public List<UpdateQuestionOptionDto> Options { get; set; } = new();
    }
}
