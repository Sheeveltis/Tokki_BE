using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.RoadmapVer2.Queries.GetTaskDetail
{
    public class GetRoadmapVer2TaskDetailQueryHandler : IRequestHandler<GetRoadmapVer2TaskDetailQuery, OperationResult<RoadmapVer2TaskDetailResult>>
    {
        private readonly IUserRoadmapRepository _userRoadmapRepository;

        public GetRoadmapVer2TaskDetailQueryHandler(IUserRoadmapRepository userRoadmapRepository)
        {
            _userRoadmapRepository = userRoadmapRepository;
        }

        public async Task<OperationResult<RoadmapVer2TaskDetailResult>> Handle(GetRoadmapVer2TaskDetailQuery request, CancellationToken cancellationToken)
        {
            var task = await _userRoadmapRepository.GetTaskByIdAsync(request.TaskId, cancellationToken);
            if (task == null)
            {
                return OperationResult<RoadmapVer2TaskDetailResult>.Failure("Không tìm thấy thông tin bài học.", 404);
            }

            var result = new RoadmapVer2TaskDetailResult
            {
                TaskId = task.TaskId,
                Title = task.Title,
                TaskType = task.TaskType.ToString(),
                Skill = task.QuestionType?.Skill.ToString(),
                IsCompleted = task.IsCompleted,
                DayIndex = task.DayIndex,
                Content = task.AiGeneratedContent,
                ExamId = task.ExamId,
                QuestionTypeId = task.QuestionTypeId ?? task.TargetQuestionTypeId
            };

            return OperationResult<RoadmapVer2TaskDetailResult>.Success(result);
        }
    }
}
