// Application/UseCases/TopikWriting/Question54/Commands/SolveQuestion54Handler.cs
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TopikWriting.Question54.DTOs;

namespace Tokki.Application.UseCases.TopikWriting.Question54.Commands
{
    public sealed class SolveQuestion54Handler
        : IRequestHandler<SolveQuestion54Command, OperationResult<Question54ResultDto>>
    {
        private readonly IQuestion54Pipeline _pipeline;

        public SolveQuestion54Handler(IQuestion54Pipeline pipeline)
        {
            _pipeline = pipeline;
        }

        public async Task<OperationResult<Question54ResultDto>> Handle(
            SolveQuestion54Command request,
            CancellationToken cancellationToken)
        {
            try
            {
                var (feedback, score) = await _pipeline.SolveAsync(request.Payload, cancellationToken);

                return OperationResult<Question54ResultDto>.Success(
                    new Question54ResultDto
                    {
                        Score = score,
                        Feedback = feedback
                    }, 200, "OK");
            }
            catch (Exception ex)
            {
                return OperationResult<Question54ResultDto>.Failure(
                    $"Lỗi xử lý câu 54: {ex.Message}", 500);
            }
        }
    }
}