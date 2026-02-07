using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Roadmap.Commands.CompleteTask
{
    public class CompleteTaskCommandHandler : IRequestHandler<CompleteTaskCommand, OperationResult<bool>>
    {
        private readonly IUserRoadmapRepository _repository;
        // private readonly IGamificationService _gamificationService; 

        public CompleteTaskCommandHandler(IUserRoadmapRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<bool>> Handle(CompleteTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repository.GetTaskByIdAsync(request.TaskId, cancellationToken);

            if (task == null)
            {
                return OperationResult<bool>.Failure("Không tìm thấy bài học.", 404);
            }

            if (task.RoadmapWeek.UserRoadmap.UserId != request.UserId)
            {
                return OperationResult<bool>.Failure("Bạn không có quyền thao tác.", 403);
            }

            if (!task.IsCompleted)
            {
                task.IsCompleted = true;

                // task.PerformanceNote = request.Performance; 

                // GAMIFICATION (Cộng điểm XP, Tăng Streak) - tạm thời chưa làm, để sau update

                await _repository.SaveChangesAsync(cancellationToken);
            }

            return OperationResult<bool>.Success(true);
        }
    }
}