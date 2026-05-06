using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Alphabet.Commands.UpdateAlphabet
{
    public class UpdateAlphabetCommandHandler : IRequestHandler<UpdateAlphabetCommand, OperationResult<bool>>
    {
        private readonly IAlphabetRepository _alphabetRepo;

        public UpdateAlphabetCommandHandler(IAlphabetRepository alphabetRepo)
        {
            _alphabetRepo = alphabetRepo;
        }

        public async Task<OperationResult<bool>> Handle(UpdateAlphabetCommand request, CancellationToken cancellationToken)
        {
            var entity = await _alphabetRepo.GetByIdAsync(request.Id);
            if (entity == null)
            {
                return OperationResult<bool>.Failure(new Error("NOT_FOUND", "Không tìm thấy dữ liệu Alphabet."));
            }

            // Kiểm tra xem letter mới có trùng với ký tự khác không
            if (!entity.Letter.Equals(request.Letter, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _alphabetRepo.GetByLetterAsync(request.Letter);
                if (existing != null)
                {
                    return OperationResult<bool>.Failure(new Error("ALREADY_EXISTS", $"Ký tự '{request.Letter}' đã tồn tại."));
                }
            }

            entity.Letter = request.Letter;
            entity.Meaning = request.Meaning;
            entity.Pronunciation = request.Pronunciation;
            entity.Type = request.Type;
            entity.AudioUrl = request.AudioUrl;
            entity.DisplayDataJson = request.DisplayDataJson;
            entity.ValidationDataJson = request.ValidationDataJson;
            entity.TotalStrokes = request.TotalStrokes;
            entity.SortOrder = request.SortOrder;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _alphabetRepo.UpdateAsync(entity);
            await _alphabetRepo.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}
