using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.VocabularyExample.Queries.GetByVocabularyId
{
    public class GetVocabularyExamplesByVocabularyIdQueryHandler
        : IRequestHandler<GetVocabularyExamplesByVocabularyIdQuery, OperationResult<List<VocabularyExampleResponse>>>
    {
        private readonly IVocabularyExampleRepository _exampleRepo;

        public GetVocabularyExamplesByVocabularyIdQueryHandler(IVocabularyExampleRepository exampleRepo)
        {
            _exampleRepo = exampleRepo;
        }

        public async Task<OperationResult<List<VocabularyExampleResponse>>> Handle(
            GetVocabularyExamplesByVocabularyIdQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.VocabularyId))
            {
                return OperationResult<List<VocabularyExampleResponse>>.Failure(
                    new List<Error> { new Error("VOCAB_ID_EMPTY", "VocabularyId không được rỗng") },
                    400,
                    "VocabularyId không được rỗng"
                );
            }

            var examples = await _exampleRepo.GetByVocabularyIdAsync(request.VocabularyId            );

            var result = examples.Select(e => new VocabularyExampleResponse
            {
                ExampleId = e.ExampleId,
                Sentence = e.Sentence,
                Translation = e.Translation
            }).ToList();

            return OperationResult<List<VocabularyExampleResponse>>.Success(
                result,
                200,
                "Lấy danh sách câu ví dụ thành công"
            );
        }
    }
}
