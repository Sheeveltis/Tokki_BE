using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.PronunciationExample.Commands.UpdatePronunciationExample
{
    public class UpdatePronunciationExampleCommandHandler : IRequestHandler<UpdatePronunciationExampleCommand, OperationResult<Unit>>
    {
        private readonly IPronunciationExampleRepository _exampleRepo;

        public UpdatePronunciationExampleCommandHandler(IPronunciationExampleRepository exampleRepo)
        {
            _exampleRepo = exampleRepo;
        }

        public async Task<OperationResult<Unit>> Handle(UpdatePronunciationExampleCommand request, CancellationToken cancellationToken)
        {
            var entity = await _exampleRepo.GetByIdAsync(request.ExampleId);
            if (entity == null)
            {
                return OperationResult<Unit>.Failure(new Error("Example.NotFound", "Ví dụ phát âm không tồn tại."), 404);
            }

            entity.TargetScript = request.TargetScript;
            entity.RawScript = request.RawScript;
            entity.PhoneticScript = request.PhoneticScript;
            entity.Meaning = request.Meaning;
            if (!string.IsNullOrEmpty(request.AudioUrl))
            {
                entity.AudioUrl = request.AudioUrl;
            }
            entity.SortOrder = request.SortOrder;
            entity.UpdateBy = request.UserId;
            entity.UpdateDate = DateTime.UtcNow;

            await _exampleRepo.UpdateAsync(entity);
            await _exampleRepo.SaveChangesAsync(cancellationToken);

            return OperationResult<Unit>.Success(Unit.Value);
        }
    }
}
