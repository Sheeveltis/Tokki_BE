using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.RoadmapVer2.Queries.GetTaskDetail
{
    public class GetRoadmapVer2TaskDetailQuery : IRequest<OperationResult<RoadmapVer2TaskDetailResult>>
    {
        public string TaskId { get; set; }

        public GetRoadmapVer2TaskDetailQuery(string taskId)
        {
            TaskId = taskId;
        }
    }

    public class RoadmapVer2TaskDetailResult
    {
        public string TaskId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty;
        public string? Skill { get; set; }
        public bool IsCompleted { get; set; }
        public int DayIndex { get; set; }
        public string? Content { get; set; }
        public string? ExamId { get; set; }
        public string? QuestionTypeId { get; set; }
    }
}
