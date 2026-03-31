using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Application.IServices;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Titles.Commands.CreateTitle
{
    public class CreateTitleCommandHandler : IRequestHandler<CreateTitleCommand, OperationResult<Title>>
    {
        private readonly ITitleRepository _titleRepository;
        private readonly IIdGeneratorService _idGenerator;

        public CreateTitleCommandHandler(ITitleRepository titleRepository, IIdGeneratorService idGenerator)
        {
            _titleRepository = titleRepository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<Title>> Handle(CreateTitleCommand request, CancellationToken cancellationToken)
        {
            // Business Logic: Duplicate Check
            var existingTitle = await _titleRepository.GetTitleByNameAsync(request.Name.Trim(), TitleStatus.Active);
            if (existingTitle != null)
            {
                return OperationResult<Title>.Failure($"Danh hiệu '{request.Name}' đã tồn tại và đang hoạt động!", 400);
            }

            // Create Entity
            string newTitleId = _idGenerator.Generate(10);
            var newTitle = new Title
            {
                TitleId = newTitleId,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                RequirementType = request.RequirementType,
                RequirementQuantity = request.RequirementQuantity,
                ColorHex = request.ColorHex.Trim(),
                IconUrl = request.IconUrl.Trim(),
                IsSystemGiven = request.IsSystemGiven
            };

            await _titleRepository.AddAsync(newTitle);

            return OperationResult<Title>.Success(newTitle, 201, "Tạo danh hiệu thành công");
        }
    }
}