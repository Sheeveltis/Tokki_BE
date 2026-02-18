// Application/UseCases/TopikWriting/Question52/Commands/SolveQuestion52Handler.cs
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TopikWriting.Question52.DTOs;

namespace Tokki.Application.UseCases.TopikWriting.Question52.Commands
{
    public sealed class SolveQuestion52Handler
        : IRequestHandler<SolveQuestion52Command, OperationResult<Question52ResultDto>>
    {
        private readonly IQuestion52Pipeline _pipeline;

        public SolveQuestion52Handler(IQuestion52Pipeline pipeline)
        {
            _pipeline = pipeline;
        }

        public async Task<OperationResult<Question52ResultDto>> Handle(
            SolveQuestion52Command request,
            CancellationToken cancellationToken)
        {
            try
            {
                var (feedback, score) = await _pipeline.SolveAsync(request.Payload, cancellationToken);

                return OperationResult<Question52ResultDto>.Success(
                    new Question52ResultDto
                    {
                        Score = score,
                        Feedback = feedback
                    }, 200, "OK");
            }
            catch (Exception ex)
            {
                return OperationResult<Question52ResultDto>.Failure(
                    $"Lỗi xử lý câu 52: {ex.Message}", 500);
            }
        }
    }
}