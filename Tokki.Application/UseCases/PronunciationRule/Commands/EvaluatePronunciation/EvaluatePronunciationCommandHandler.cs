using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.PronunciationRule.DTOs;

namespace Tokki.Application.UseCases.PronunciationRule.Commands.EvaluatePronunciation
{
    public class EvaluatePronunciationCommandHandler : IRequestHandler<EvaluatePronunciationCommand, OperationResult<PronunciationResponse>>
    {
        private readonly ISpeechService _speechService;
        private readonly IAIPronunciationService _aiService; 
        private readonly IPronunciationRuleRepository _ruleRepository;
        private readonly IPronunciationExampleRepository _exampleRepository;
        public EvaluatePronunciationCommandHandler(
            ISpeechService speechService,
            IAIPronunciationService aiService,
            IPronunciationRuleRepository ruleRepository,
            IPronunciationExampleRepository exampleRepository)
        {
            _speechService = speechService;
            _aiService = aiService;
            _ruleRepository = ruleRepository;
            _exampleRepository = exampleRepository;
        }

        public async Task<OperationResult<PronunciationResponse>> Handle(EvaluatePronunciationCommand request, CancellationToken cancellationToken)
        {
            var example = await _exampleRepository.GetByIdAsync(request.ExampleId);
            if (example == null)
            {
                return OperationResult<PronunciationResponse>.Failure(new Error("EXAMPLE_NOT_FOUND", "Không tìm thấy nội dung mẫu."));
            }

            using var stream = request.AudioFile.OpenReadStream();
            var azureResult = await _speechService.AssessPronunciationAsync(stream, example.RawScript);

            if (azureResult.CompletenessScore < 30 || azureResult.AccuracyScore < 20)
            {
                return OperationResult<PronunciationResponse>.Success(new PronunciationResponse
                {
                    IsIrrelevant = true,
                    AccuracyScore = azureResult.AccuracyScore,
                    AiFeedback = "Tokki nhận thấy bạn đọc chưa đúng câu mẫu, hoặc âm thanh quá ồn. Hãy thử lại to rõ hơn nhé!",
                    Words = new List<WordAssessmentDTO>()
                });
            }

            var rule = await _ruleRepository.GetByIdAsync(example.PronunciationRuleId);
            string ruleContext = rule != null ? $"{rule.RuleName}: {rule.Description}" : "Quy tắc phát âm cơ bản";

            var (feedback, finalScore) = await _aiService.GenerateFeedbackAsync(
                azureResult,
                example.RawScript,
                ruleContext);

            return OperationResult<PronunciationResponse>.Success(new PronunciationResponse
            {
                IsIrrelevant = false,
                AccuracyScore = finalScore,
                AiFeedback = feedback,
                Words = azureResult.Words,
                FluencyScore = azureResult.FluencyScore,
                CompletenessScore = azureResult.CompletenessScore,
                ProsodyScore = azureResult.ProsodyScore
            });
        }
    }
}
