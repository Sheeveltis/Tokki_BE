using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.PronunciationExample.DTOs;

namespace Tokki.Application.UseCases.PronunciationExample.Queries.GetExamplesByRuleId
{
    public class GetExamplesByRuleIdQueryHandler : IRequestHandler<GetExamplesByRuleIdQuery, OperationResult<List<ExampleSimpleDTO>>>
    {
        private readonly IPronunciationExampleRepository _exampleRepository;

        public GetExamplesByRuleIdQueryHandler(IPronunciationExampleRepository exampleRepository)
        {
            _exampleRepository = exampleRepository;
        }

        public async Task<OperationResult<List<ExampleSimpleDTO>>> Handle(GetExamplesByRuleIdQuery request, CancellationToken cancellationToken)
        {
            var examples = await _exampleRepository.GetExamplesByRuleIdAsync(request.PronunciationRuleId, cancellationToken);

            var dtoList = examples.Select(e => new ExampleSimpleDTO
            {
                ExampleId = e.ExampleId,
                RawScript = e.RawScript,
                SortOrder = e.SortOrder,
                Difficulty = e.Difficulty.ToString()
            }).ToList();

            return OperationResult<List<ExampleSimpleDTO>>.Success(dtoList, 200, "Lấy danh sách bài tập thành công.");
        }
    }
}
