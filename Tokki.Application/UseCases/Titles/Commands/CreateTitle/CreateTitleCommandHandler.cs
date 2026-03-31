using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Application.IServices; 

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
            var existingTitle = await _titleRepository.GetTitleByNameAsync(request.Name);
            if (existingTitle != null)
            {
                return OperationResult<Title>.Failure(new List<Error>(), 400, $"Danh hiệu '{request.Name}' đã tồn tại!");
            }

            if (request.RequirementQuantity < 0)
            {
                return OperationResult<Title>.Failure(new List<Error>(), 400, "Giá trị điều kiện không được âm.");
            }
            
            string newTitleId = _idGenerator.Generate(10);
            var newTitle = new Title
            {
                TitleId = newTitleId,
                Name = request.Name,
                Description = request.Description,
                RequirementType = request.RequirementType,
                RequirementQuantity = request.RequirementQuantity,
                ColorHex = request.ColorHex,
                IconUrl = request.IconUrl,
                IsSystemGiven = request.IsSystemGiven
            };

            await _titleRepository.AddAsync(newTitle);

            return OperationResult<Title>.Success(newTitle, 201, "Tạo danh hiệu thành công");
        }
    }
}