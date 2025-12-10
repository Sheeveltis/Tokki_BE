using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Titles.Commands.CreateTitle
{
    public class CreateTitleCommandHandler : IRequestHandler<CreateTitleCommand, OperationResult<Title>>
    {
        private readonly ITitleRepository _titleRepository;

        public CreateTitleCommandHandler(ITitleRepository titleRepository)
        {
            _titleRepository = titleRepository;
        }

        public async Task<OperationResult<Title>> Handle(CreateTitleCommand request, CancellationToken cancellationToken)
        {
            var existingTitle = await _titleRepository.GetTitleByNameAsync(request.Name);
            if (existingTitle != null)
            {
                return OperationResult<Title>.Failure(new List<Error>(), 400, $"Danh hiệu '{request.Name}' đã tồn tại!");
            }

            if (request.RequiredXP < 0)
            {
                return OperationResult<Title>.Failure(new List<Error>(), 400, "XP không được âm.");
            }

            var newTitle = new Title
            {
                Name = request.Name,
                Description = request.Description,
                RequiredXP = request.RequiredXP,
                ColorHex = request.ColorHex,
                IconUrl = request.IconUrl,
                IsSystemGiven = request.IsSystemGiven
            };

            await _titleRepository.AddAsync(newTitle);

            return OperationResult<Title>.Success(newTitle, 201, "Tạo danh hiệu thành công");
        }
    }
}