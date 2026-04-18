using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.PronunciationExample.DTOs;

namespace Tokki.Application.UseCases.PronunciationExample.Queries.GetExampleDetail
{
    public class GetExampleDetailQueryHandler : IRequestHandler<GetExampleDetailQuery, OperationResult<ExampleDetailDTO>>
    {
        private readonly IPronunciationExampleRepository _exampleRepository;

        public GetExampleDetailQueryHandler(IPronunciationExampleRepository exampleRepository)
        {
            _exampleRepository = exampleRepository;
        }

        public async Task<OperationResult<ExampleDetailDTO>> Handle(GetExampleDetailQuery request, CancellationToken cancellationToken)
        {
            var example = await _exampleRepository.GetDetailByIdAsync(request.ExampleId, cancellationToken);

            if (example == null)
            {
                return OperationResult<ExampleDetailDTO>.Failure(new Error("NOT_FOUND", "Không tìm thấy nội dung bài học."));
            }

            var dto = new ExampleDetailDTO
            {
                ExampleId = example.ExampleId,
                TargetScript = example.TargetScript,
                RawScript = example.RawScript,
                PhoneticScript = example.PhoneticScript,
                Meaning = example.Meaning ?? string.Empty,
                AudioUrl = example.AudioUrl ?? string.Empty,
                RuleName = example.PronunciationRule?.RuleName ?? string.Empty,
                RuleDescription = example.PronunciationRule?.Description ?? "",
                RuleContent = example.PronunciationRule?.Content ?? "",
                SortOrder = example.SortOrder,
                Difficulty = example.Difficulty.ToString()
            };

            return OperationResult<ExampleDetailDTO>.Success(dto, 200, "Lấy chi tiết bài học thành công.");
        }
    }
}
