using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.PronunciationExample.DTOs;

namespace Tokki.Application.UseCases.PronunciationExample.Queries.GetExamplesByRuleId
{
    public class GetExamplesByRuleIdQuery : IRequest<OperationResult<List<ExampleSimpleDTO>>>
    {
        public string PronunciationRuleId { get; set; }
        public GetExamplesByRuleIdQuery(string exampleId)
        {
            PronunciationRuleId = exampleId;
        }
    }
}
