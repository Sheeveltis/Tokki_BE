using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Passages.Commands.CreatePassage
{
    public class CreatePassageCommandHandler : IRequestHandler<CreatePassageCommand, OperationResult<string>>
    {
        private readonly IPassageRepository _passageRepository;
        private readonly IIdGeneratorService _idGenerator;

        public CreatePassageCommandHandler(IPassageRepository passageRepository, IIdGeneratorService idGenerator)
        {
            _passageRepository = passageRepository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<string>> Handle(CreatePassageCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var title = request.Title.Trim();

                var isTitleExists = await _passageRepository.IsTitleExistsAsync(title);
                if (isTitleExists)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.PassageTitleDuplicated },
                        409,
                        AppErrors.PassageTitleDuplicated.Description
                    );
                }

                var passageId = _idGenerator.GenerateCustom(10);

                var passage = new Passage
                {
                    PassageId = passageId,
                    Title = title,
                    Content = request.Content,
                    ImageUrl = request.ImageUrl,
                    MediaType = request.MediaType,
                    Status = PassageStatus.Active,

                    // Nếu DB đã default CreatedAt thì bạn có thể bỏ dòng này.
                    CreatedAt = DateTime.UtcNow.AddHours(7)
                };

                await _passageRepository.AddAsync(passage);
                await _passageRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(passageId, 201, "Tạo đoạn văn thành công.");
            }
            catch
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
