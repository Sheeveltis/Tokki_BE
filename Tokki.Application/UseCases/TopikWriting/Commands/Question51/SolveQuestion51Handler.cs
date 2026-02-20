// Application/UseCases/TopikWriting/Question51/Commands/SolveQuestion51Handler.cs
using MediatR;
using Hangfire;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TopikWriting.Question51.DTOs;
using System.Text.Json;
using Tokki.Application.UseCases.TopikWriting.DTOs;

namespace Tokki.Application.UseCases.TopikWriting.Question51.Commands
{
    public sealed class SolveQuestion51Handler
        : IRequestHandler<SolveQuestion51Command, OperationResult<Question51ResultDto>>
    {
        private readonly IBackgroundJobClient _backgroundJobs;

        public SolveQuestion51Handler(IBackgroundJobClient backgroundJobs)
        {
            _backgroundJobs = backgroundJobs;
        }

        public Task<OperationResult<Question51ResultDto>> Handle(
            SolveQuestion51Command request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Enqueue background job - chạy ngầm
                var jobId = _backgroundJobs.Enqueue<IWritingGradingBackgroundService>(
                    service => service.GradeQuestion51Async(request.Payload.UserExamWritingAnswerId));

                // Trả về ngay cho FE - không chờ AI
                var result = new Question51ResultDto
                {
                    Score = -1, // -1 nghĩa là "đang chấm"
                    Feedback = JsonDocument.Parse("{\"status\":\"grading\",\"message\":\"Đang chấm bài, vui lòng đợi...\"}").RootElement
                };

                return Task.FromResult(OperationResult<Question51ResultDto>.Success(
                    result, 202, "Bài làm đang được chấm điểm, vui lòng kiểm tra lại sau"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(OperationResult<Question51ResultDto>.Failure(
                    $"Lỗi xử lý câu 51: {ex.Message}", 500));
            }
        }
    }
}