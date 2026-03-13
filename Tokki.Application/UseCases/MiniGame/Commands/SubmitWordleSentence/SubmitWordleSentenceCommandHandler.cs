using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.MiniGame.DTOs;
using Tokki.Domain.Entities;
using System.Text.Json;
namespace Tokki.Application.UseCases.MiniGame.Commands.SubmitWordleSentence
{
    public class SubmitWordleSentenceCommandHandler : IRequestHandler<SubmitWordleSentenceCommand, OperationResult<WordleSubmissionResponse>>
    {
        private readonly IWordleRepository _wordleRepo; 
        private readonly IAIWordleService _aiWordleService;
        private readonly IIdGeneratorService _idGenerator;

        public SubmitWordleSentenceCommandHandler(IWordleRepository wordleRepo, IAIWordleService aiWordleService, IIdGeneratorService idGeneratorService)
        {
            _wordleRepo = wordleRepo;
            _aiWordleService = aiWordleService;
            _idGenerator = idGeneratorService;
        }

        public async Task<OperationResult<WordleSubmissionResponse>> Handle(SubmitWordleSentenceCommand request, CancellationToken token)
        {
            var dailyWordle = await _wordleRepo.GetDailyWordleWithVocabAsync(request.DailyWordleId, token);
            if (dailyWordle == null || dailyWordle.Vocabulary == null)
                return OperationResult<WordleSubmissionResponse>.Failure("Không tìm thấy từ vựng Wordle ngày này.", 404);

            var vocab = dailyWordle.Vocabulary;

            var aiFeedback = await _aiWordleService.EvaluateSentenceAsync(
                request.SentenceContent,
                vocab.Text,
                vocab.Definition);

            var submission = new WordleSentenceSubmission
            {
                SubmissionId = _idGenerator.Generate(20) ,
                UserId = request.UserId,
                DailyWordleId = request.DailyWordleId,
                SentenceContent = request.SentenceContent,
                AiScore = (int)Math.Round(aiFeedback.TotalScore),
                AiFeedbackJson = JsonSerializer.Serialize(aiFeedback),
                IsPublic = false,
                IsAnonymous = false,
                CreatedAt = DateTime.UtcNow
            };

            await _wordleRepo.AddSubmissionAsync(submission, token);
            return OperationResult<WordleSubmissionResponse>.Success(new WordleSubmissionResponse
            {
                SubmissionId = submission.SubmissionId, 
                TargetWord = vocab.Text,
                Meaning = vocab.Definition,
                AiFeedback = aiFeedback
            });
        }
    }
}
