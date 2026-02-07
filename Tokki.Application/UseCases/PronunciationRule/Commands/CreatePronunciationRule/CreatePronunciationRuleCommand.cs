using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.PronunciationRule.Commands.CreatePronunciationRule
{
    public class CreatePronunciationRuleCommand : IRequest<OperationResult<string>>
    {
        public string RuleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Content { get; set; } 

        [JsonIgnore]
        public string? CreateBy { get; set; }
    }
}
