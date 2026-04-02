// Application/UseCases/TopikWriting/Question53/Commands/SolveQuestion53Handler.cs
using MediatR;
using Hangfire;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TopikWriting.Question53.DTOs;
using System.Text.Json;

namespace Tokki.Application.UseCases.TopikWriting.Question53.Commands
{
    public sealed class SolveQuestion53Handler
        : IRequestHandler<SolveQuestion53Command, OperationResult<Question53ResultDto>>
    {
        private readonly IBackgroundJobClient _backgroundJobs;

        public SolveQuestion53Handler(IBackgroundJobClient backgroundJobs)
        {
            _backgroundJobs = backgroundJobs;
        }

        public Task<OperationResult<Question53ResultDto>> Handle(
            SolveQuestion53Command request,
            CancellationToken cancellationToken)
        {
            try
            {
                _backgroundJobs.Enqueue<IWritingGradingBackgroundService>(
      service => service.GradeQuestion53Async(request.Payload.UserExamWritingAnswerId));
                var result = new Question53ResultDto
                {
                    Score = -1,
                    Feedback = JsonDocument.Parse("{\"status\":\"grading\",\"message\":\"Đang chấm bài, vui lòng đợi...\"}").RootElement
                };

                return Task.FromResult(OperationResult<Question53ResultDto>.Success(
                    result, 202, "Bài làm đang được chấm điểm, vui lòng kiểm tra lại sau"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(OperationResult<Question53ResultDto>.Failure(
                    $"Lỗi xử lý câu 53: {ex.Message}", 500));
            }
        }
    }
}