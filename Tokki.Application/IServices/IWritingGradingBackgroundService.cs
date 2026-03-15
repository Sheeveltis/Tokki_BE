// Application/IServices/IWritingGradingBackgroundService.cs
using Hangfire.Server;

namespace Tokki.Application.IServices
{
    public interface IWritingGradingBackgroundService
    {
        Task GradeQuestion51Async(string userExamWritingAnswerId);
        Task GradeQuestion52Async(string userExamWritingAnswerId);
        Task GradeQuestion53Async(string userExamWritingAnswerId);
        Task GradeQuestion54Async(string userExamWritingAnswerId);
    }
}