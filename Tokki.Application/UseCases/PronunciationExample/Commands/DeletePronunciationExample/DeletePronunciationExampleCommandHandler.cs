using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.PronunciationExample.Commands.DeletePronunciationExample
{
    public class DeletePronunciationExampleCommandHandler : IRequestHandler<DeletePronunciationExampleCommand, OperationResult<Unit>>
    {
        private readonly IPronunciationExampleRepository _exampleRepo;

        public DeletePronunciationExampleCommandHandler(IPronunciationExampleRepository exampleRepo)
        {
            _exampleRepo = exampleRepo;
        }

        public async Task<OperationResult<Unit>> Handle(DeletePronunciationExampleCommand request, CancellationToken cancellationToken)
        {
            var entity = await _exampleRepo.GetByIdAsync(request.ExampleId);
            if (entity == null)
            {
                return OperationResult<Unit>.Failure(new Error("Example.NotFound", "Ví dụ phát âm không tồn tại."), 404);
            }

            await _exampleRepo.DeleteAsync(entity);
            await _exampleRepo.SaveChangesAsync(cancellationToken);

            return OperationResult<Unit>.Success(Unit.Value);
        }
    }
}
