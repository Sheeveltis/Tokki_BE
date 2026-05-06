using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Alphabet.Commands.ToggleAlphabetStatus
{
    public class ToggleAlphabetStatusCommandHandler : IRequestHandler<ToggleAlphabetStatusCommand, OperationResult<bool>>
    {
        private readonly IAlphabetRepository _alphabetRepo;

        public ToggleAlphabetStatusCommandHandler(IAlphabetRepository alphabetRepo)
        {
            _alphabetRepo = alphabetRepo;
        }

        public async Task<OperationResult<bool>> Handle(ToggleAlphabetStatusCommand request, CancellationToken cancellationToken)
        {
            var entity = await _alphabetRepo.GetByIdAsync(request.Id);
            if (entity == null)
            {
                return OperationResult<bool>.Failure(new Error("NOT_FOUND", "Không tìm thấy dữ liệu Alphabet."));
            }

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _alphabetRepo.UpdateAsync(entity);
            await _alphabetRepo.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(entity.IsActive);
        }
    }
}
