using MediatR;
using Tokki.Application.Common.Models;

public class GetTaskDetailQueryHandler
    : IRequestHandler<GetTaskDetailQuery, OperationResult<TaskDetailResult>>
{
    private readonly IUserRoadmapRepository _repository;

    public GetTaskDetailQueryHandler(IUserRoadmapRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<TaskDetailResult>> Handle(
        GetTaskDetailQuery request,
        CancellationToken cancellationToken)
    {
        var task = await _repository.GetTaskByIdAsync(
            request.TaskId, cancellationToken);

        if (task == null)
            return OperationResult<TaskDetailResult>.Failure(
                "Không tìm thấy task.", 404);

        var result = new TaskDetailResult
        {
            TaskId = task.TaskId,
            Title = task.Title,
            TaskType = task.TaskType.ToString(),
            Skill = task.QuestionType != null ? task.QuestionType.Skill.ToString() : null,
            IsCompleted = task.IsCompleted,
            DayIndex = task.DayIndex,
            Content = task.AiGeneratedContent,
            ExamId = task.ExamId,
            QuestionTypeId = task.QuestionTypeId
        };

        return OperationResult<TaskDetailResult>.Success(result);
    }
}