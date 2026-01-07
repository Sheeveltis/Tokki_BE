using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Queries.GetQuestionBanks
{
    public class GetQuestionBanksQuery : IRequest<OperationResult<PagedResult<QuestionBankDto>>>
    {
        public string? SearchTerm { get; set; }
        public QuestionSkill? Skill { get; set; }
        public DifficultyLevel? DifficultyLevel { get; set; }
        public string? QuestionTypeId { get; set; }
        public string? PassageId { get; set; }
        public QuestionBankStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
