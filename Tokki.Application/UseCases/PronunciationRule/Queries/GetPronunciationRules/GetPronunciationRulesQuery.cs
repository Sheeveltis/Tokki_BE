using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.PronunciationRule.DTOs;

namespace Tokki.Application.UseCases.PronunciationRule.Queries.GetPronunciationRules
{
    public class GetPronunciationRulesQuery : IRequest<OperationResult<List<PronunciationRuleDTO>>> { }
}
