using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Alphabet.Commands.CreateAlphabet
{
    public class CreateAlphabetCommandHandler : IRequestHandler<CreateAlphabetCommand, OperationResult<int>>
    {
        private readonly IAlphabetRepository _alphabetRepo;

        public CreateAlphabetCommandHandler(IAlphabetRepository alphabetRepo)
        {
            _alphabetRepo = alphabetRepo;
        }

        public async Task<OperationResult<int>> Handle(CreateAlphabetCommand request, CancellationToken cancellationToken)
        {
            var existing = await _alphabetRepo.GetByLetterAsync(request.Letter);
            if (existing != null)
            {
                return OperationResult<int>.Failure(new Error("ALREADY_EXISTS", $"Ký tự '{request.Letter}' đã tồn tại."));
            }

            var entity = new AlphabetData
            {
                Letter = request.Letter,
                Meaning = request.Meaning,
                Pronunciation = request.Pronunciation,
                Type = request.Type,
                AudioUrl = request.AudioUrl,
                DisplayDataJson = request.DisplayDataJson,
                ValidationDataJson = request.ValidationDataJson,
                TotalStrokes = request.TotalStrokes,
                SortOrder = request.SortOrder,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow.AddHours(7),
                UpdatedAt = DateTime.UtcNow.AddHours(7)
            };

            await _alphabetRepo.AddAsync(entity);
            await _alphabetRepo.SaveChangesAsync(cancellationToken);

            return OperationResult<int>.Success(entity.Id, 201);
        }
    }
}
