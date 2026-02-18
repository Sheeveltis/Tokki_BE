using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TopikWriting.DTOs;
namespace Tokki.Application.UseCases.TopikWriting.Question51.Commands
{
    public sealed class SolveQuestion51Handler
        : IRequestHandler<SolveQuestion51Command, OperationResult<Question51ResultDto>>
    {
        private readonly IQuestion51Pipeline _pipeline;

        public SolveQuestion51Handler(IQuestion51Pipeline pipeline)
        {
            _pipeline = pipeline;
        }

        public async Task<OperationResult<Question51ResultDto>> Handle(
            SolveQuestion51Command request,
            CancellationToken cancellationToken)
        {
            try
            {
                var (feedback, score) = await _pipeline.SolveAsync(request.Payload, cancellationToken);

                return OperationResult<Question51ResultDto>.Success(
                    new Question51ResultDto
                    {
                        Score = score,
                        Feedback = feedback
                    },
                    200, "OK");
            }
            catch (Exception ex)
            {
                return OperationResult<Question51ResultDto>.Failure(
                    $"Lỗi xử lý câu 51: {ex.Message}", 500);
            }
        }
    }
}