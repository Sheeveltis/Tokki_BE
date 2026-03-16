using System.Text.Json.Serialization;
using MediatR;
using Tokki.Application.Common.Models;

public class GetTaskDetailQuery : IRequest<OperationResult<TaskDetailResult>>
{
    public string TaskId { get; set; } = string.Empty;
}

public class TaskDetailResult
{
    public string TaskId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string Skill { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int DayIndex { get; set; }
    public string? Content { get; set; }        
    public string? ExamId { get; set; }
    public string? QuestionTypeId { get; set; }
}