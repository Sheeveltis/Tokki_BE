// Application/UseCases/TopikWriting/Question54/Commands/SolveQuestion54Handler.cs
using MediatR;
using Hangfire;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TopikWriting.Question54.DTOs;
using System.Text.Json;

namespace Tokki.Application.UseCases.TopikWriting.Question54.Commands
{
    public sealed class SolveQuestion54Handler
        : IRequestHandler<SolveQuestion54Command, OperationResult<Question54ResultDto>>
    {
        private readonly IBackgroundJobClient _backgroundJobs;

        public SolveQuestion54Handler(IBackgroundJobClient backgroundJobs)
        {
            _backgroundJobs = backgroundJobs;
        }

        public Task<OperationResult<Question54ResultDto>> Handle(
            SolveQuestion54Command request,
            CancellationToken cancellationToken)
        {
            try
            {
                var jobId = _backgroundJobs.Enqueue<IWritingGradingBackgroundService>(
                    service => service.GradeQuestion54Async(request.Payload.UserExamWritingAnswerId));

                var result = new Question54ResultDto
                {
                    Score = -1,
                    Feedback = JsonDocument.Parse("{\"status\":\"grading\",\"message\":\"Đang chấm bài, vui lòng đợi...\"}").RootElement
                };

                return Task.FromResult(OperationResult<Question54ResultDto>.Success(
                    result, 202, "Bài làm đang được chấm điểm, vui lòng kiểm tra lại sau"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(OperationResult<Question54ResultDto>.Failure(
                    $"Lỗi xử lý câu 54: {ex.Message}", 500));
            }
        }
    }
}