//using MediatR;
//using Tokki.Application.Common.Models;
//using Tokki.Application.IServices;
//using Tokki.Application.UseCases.TopikWriting.DTOs;

//namespace Tokki.Application.UseCases.TopikWriting.Commands.ClassifyAndSolve
//{
//    public sealed class ClassifyAndSolveTopikWritingHandler
//        : IRequestHandler<ClassifyAndSolveTopikWritingCommand, OperationResult<TopikWritingResultDto>>
//    {
//        private readonly ITopikWritingGeminiPipeline _pipeline;

//        public ClassifyAndSolveTopikWritingHandler(ITopikWritingGeminiPipeline pipeline)
//        {
//            _pipeline = pipeline;
//        }

//        public async Task<OperationResult<TopikWritingResultDto>> Handle(
//            ClassifyAndSolveTopikWritingCommand request,
//            CancellationToken cancellationToken)
//        {
//            try
//            {
//                var feedback = await _pipeline.SolveAsync(request.Payload, cancellationToken);

//                var result = new TopikWritingResultDto
//                {
//                    Feedback = feedback
//                };

//                return OperationResult<TopikWritingResultDto>.Success(result, 200, "OK");
//            }
//            catch (Exception ex)
//            {
//                return OperationResult<TopikWritingResultDto>.Failure($"Gemini lỗi: {ex.Message}", 500);
//            }
//        }
//    }
//}