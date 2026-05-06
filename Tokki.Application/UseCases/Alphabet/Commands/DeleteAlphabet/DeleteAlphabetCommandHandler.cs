using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Alphabet.Commands.DeleteAlphabet
{
    public class DeleteAlphabetCommandHandler : IRequestHandler<DeleteAlphabetCommand, OperationResult<bool>>
    {
        private readonly IAlphabetRepository _alphabetRepo;

        public DeleteAlphabetCommandHandler(IAlphabetRepository alphabetRepo)
        {
            _alphabetRepo = alphabetRepo;
        }

        public async Task<OperationResult<bool>> Handle(DeleteAlphabetCommand request, CancellationToken cancellationToken)
        {
            var entity = await _alphabetRepo.GetByIdAsync(request.Id);
            if (entity == null)
            {
                return OperationResult<bool>.Failure(new Error("NOT_FOUND", "Không tìm thấy dữ liệu Alphabet để xóa."));
            }

            await _alphabetRepo.DeleteAsync(entity);
            await _alphabetRepo.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}
