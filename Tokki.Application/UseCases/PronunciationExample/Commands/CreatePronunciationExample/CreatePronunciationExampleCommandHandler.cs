using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.PronunciationExample.Commands.CreatePronunciationExample
{
    public class CreatePronunciationExampleCommandHandler : IRequestHandler<CreatePronunciationExampleCommand, OperationResult<string>>
    {
        private readonly IPronunciationExampleRepository _exampleRepo;
        private readonly IPronunciationRuleRepository _ruleRepo;
        private readonly IIdGeneratorService _idGenerator;

        public CreatePronunciationExampleCommandHandler(
            IPronunciationExampleRepository exampleRepo,
            IPronunciationRuleRepository ruleRepo,
            IIdGeneratorService idGenerator)
        {
            _exampleRepo = exampleRepo;
            _ruleRepo = ruleRepo;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<string>> Handle(CreatePronunciationExampleCommand request, CancellationToken cancellationToken)
        {
            var rule = await _ruleRepo.GetByIdAsync(request.PronunciationRuleId);
            if (rule == null)
            {
                return OperationResult<string>.Failure(new Error("Rule.NotFound", "Quy tắc phát âm không tồn tại."), 404);
            }

            var maxSortOrder = await _exampleRepo.GetMaxSortOrderAsync(request.PronunciationRuleId, cancellationToken);

            var entity = new Domain.Entities.PronunciationExample
            {
                ExampleId = _idGenerator.Generate(10),
                PronunciationRuleId = request.PronunciationRuleId,
                TargetScript = request.TargetScript,
                RawScript = request.RawScript,
                PhoneticScript = request.PhoneticScript,
                Meaning = request.Meaning,
                AudioUrl = request.AudioUrl,
                SortOrder = maxSortOrder + 1,
                Difficulty = request.Difficulty,
                CreateBy = request.UserId,
                CreateDate = DateTime.UtcNow
            };

            await _exampleRepo.AddAsync(entity);
            await _exampleRepo.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(entity.ExampleId, 201);
        }
    }
}
