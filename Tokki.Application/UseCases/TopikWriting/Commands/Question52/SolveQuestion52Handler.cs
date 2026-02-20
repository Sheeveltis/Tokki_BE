// Application/UseCases/TopikWriting/Question52/Commands/SolveQuestion52Handler.cs
using MediatR;
using Hangfire;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TopikWriting.Question52.DTOs;
using System.Text.Json;

namespace Tokki.Application.UseCases.TopikWriting.Question52.Commands
{
    public sealed class SolveQuestion52Handler
        : IRequestHandler<SolveQuestion52Command, OperationResult<Question52ResultDto>>
    {
        private readonly IBackgroundJobClient _backgroundJobs;

        public SolveQuestion52Handler(IBackgroundJobClient backgroundJobs)
        {
            _backgroundJobs = backgroundJobs;
        }

        public Task<OperationResult<Question52ResultDto>> Handle(
            SolveQuestion52Command request,
            CancellationToken cancellationToken)
        {
            try
            {
                var jobId = _backgroundJobs.Enqueue<IWritingGradingBackgroundService>(
                    service => service.GradeQuestion52Async(request.Payload.UserExamWritingAnswerId));

                var result = new Question52ResultDto
                {
                    Score = -1,
                    Feedback = JsonDocument.Parse("{\"status\":\"grading\",\"message\":\"Đang chấm bài, vui lòng đợi...\"}").RootElement
                };

                return Task.FromResult(OperationResult<Question52ResultDto>.Success(
                    result, 202, "Bài làm đang được chấm điểm, vui lòng kiểm tra lại sau"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(OperationResult<Question52ResultDto>.Failure(
                    $"Lỗi xử lý câu 52: {ex.Message}", 500));
            }
        }
    }
}