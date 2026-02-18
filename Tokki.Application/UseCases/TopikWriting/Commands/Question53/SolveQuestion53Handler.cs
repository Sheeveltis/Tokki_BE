// Application/UseCases/TopikWriting/Question53/Commands/SolveQuestion53Handler.cs
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TopikWriting.Question53.DTOs;

namespace Tokki.Application.UseCases.TopikWriting.Question53.Commands
{
    public sealed class SolveQuestion53Handler
        : IRequestHandler<SolveQuestion53Command, OperationResult<Question53ResultDto>>
    {
        private readonly IQuestion53Pipeline _pipeline;

        public SolveQuestion53Handler(IQuestion53Pipeline pipeline)
        {
            _pipeline = pipeline;
        }

        public async Task<OperationResult<Question53ResultDto>> Handle(
            SolveQuestion53Command request,
            CancellationToken cancellationToken)
        {
            try
            {
                var (feedback, score) = await _pipeline.SolveAsync(request.Payload, cancellationToken);

                return OperationResult<Question53ResultDto>.Success(
                    new Question53ResultDto
                    {
                        Score = score,
                        Feedback = feedback
                    }, 200, "OK");
            }
            catch (Exception ex)
            {
                return OperationResult<Question53ResultDto>.Failure(
                    $"Lỗi xử lý câu 53: {ex.Message}", 500);
            }
        }
    }
}