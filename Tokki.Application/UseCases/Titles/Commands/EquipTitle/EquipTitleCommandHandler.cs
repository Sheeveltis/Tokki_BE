using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;

namespace Tokki.Application.UseCases.Titles.Commands.EquipTitle
{
    public class EquipTitleCommandHandler : IRequestHandler<EquipTitleCommand, OperationResult<bool>>
    {
        private readonly IUserTitleService _userTitleService;

        public EquipTitleCommandHandler(IUserTitleService userTitleService)
        {
            _userTitleService = userTitleService;
        }

        public async Task<OperationResult<bool>> Handle(EquipTitleCommand request, CancellationToken cancellationToken)
        {
            // Thực hiện trang bị danh hiệu (UserTitleService sẽ kiểm tra Ownership trong AccountTitles)
            var success = await _userTitleService.EquipTitleAsync(request.UserId, request.TitleId);
            
            if (!success) 
            {
                return OperationResult<bool>.Failure("Bạn chưa sở hữu danh hiệu này hoặc danh hiệu không tồn tại.", 400);
            }

            return OperationResult<bool>.Success(true, 200, "Trang bị danh hiệu thành công.");
        }
    }
}
