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
        private readonly IAIPronunciationService _aiService; // <-- Interface mới
        private readonly IPronunciationRuleRepository _ruleRepository;

        public EvaluatePronunciationCommandHandler(
            ISpeechService speechService,
            IAIPronunciationService aiService,
            IPronunciationRuleRepository ruleRepository)
        {
            _speechService = speechService;
            _aiService = aiService;
            _ruleRepository = ruleRepository;
        }

        public async Task<OperationResult<PronunciationResponse>> Handle(EvaluatePronunciationCommand request, CancellationToken cancellationToken)
        {
            if (request.AudioFile == null || request.AudioFile.Length == 0)
                return OperationResult<PronunciationResponse>.Failure("File không hợp lệ.", 400);

            var rule = await _ruleRepository.GetByIdAsync(request.PronunciationRuleId);
            string ruleContext = rule != null ? $"{rule.RuleName}: {rule.Description}" : "Không có quy tắc cụ thể";

            try
            {
                using var stream = request.AudioFile.OpenReadStream();
                var assessmentResult = await _speechService.AssessPronunciationAsync(stream, request.RawText);

                string aiFeedback = await _aiService.GenerateFeedbackAsync(assessmentResult, request.RawText, ruleContext);

                var response = new PronunciationResponse
                {
                    AccuracyScore = assessmentResult.AccuracyScore,
                    FluencyScore = assessmentResult.FluencyScore,
                    CompletenessScore = assessmentResult.CompletenessScore,
                    ProsodyScore = assessmentResult.ProsodyScore,
                    Words = assessmentResult.Words,

                    AiFeedback = aiFeedback
                };

                return OperationResult<PronunciationResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return OperationResult<PronunciationResponse>.Failure($"Lỗi: {ex.Message}", 500);
            }
        }
    }
}
