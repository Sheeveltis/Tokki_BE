using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.UserExam.Commands.CreateUserTakeExam
{
    public class CreateUserTakeExamCommandHandler : IRequestHandler<CreateUserTakeExamCommand, OperationResult<UserTakeExamResponse>>
    {
        private readonly IUserExamRepository _repository;
        private readonly IIdGeneratorService _idGenerator;

        public CreateUserTakeExamCommandHandler(IUserExamRepository repository, IIdGeneratorService idGenerator)
        {
            _repository = repository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<UserTakeExamResponse>> Handle(CreateUserTakeExamCommand request, CancellationToken cancellationToken)
        {
            var existingSession = await _repository.GetInProgressSessionAsync(request.UserId, request.ExamId, cancellationToken);
            if (existingSession != null)
            {
                return OperationResult<UserTakeExamResponse>.Success(MapToResponse(existingSession, request.IsShuffle));
            }

            var originalExam = await _repository.GetExamWithFullStructureAsync(request.ExamId, cancellationToken);
            if (originalExam == null) return OperationResult<UserTakeExamResponse>.Failure(AppErrors.ExamNotFound, 404);

            var newSession = new Domain.Entities.UserExam
            {
                UserExamId = _idGenerator.Generate(15),
                UserId = request.UserId,
                ExamId = originalExam.ExamId,
                StartTime = DateTime.UtcNow,
                Status = UserExamStatus.InProgress,
                Score = 0,
                Exam = originalExam
            };

            var parts = originalExam.ExamTemplate.TemplateParts;
            var examQuestions = originalExam.ExamQuestions.OrderBy(eq => eq.QuestionNo).ToList();

            foreach (var eq in examQuestions)
            {
                var part = parts.FirstOrDefault(p => eq.QuestionNo >= p.QuestionFrom && eq.QuestionNo <= p.QuestionTo);
                var skill = part?.Skill ?? QuestionSkill.Reading;

                if (skill == QuestionSkill.Writing)
                {
                    newSession.UserExamWritingAnswers.Add(new UserExamWritingAnswer
                    {
                        UserExamWritingAnswerId = _idGenerator.Generate(20),
                        UserExamId = newSession.UserExamId,
                        QuestionId = eq.QuestionBankId,
                        OrderIndex = eq.QuestionNo,
                        AnswerContent = string.Empty,
                        Question = eq.QuestionBank
                    });
                }
                else
                {
                    newSession.UserExamAnswers.Add(new UserExamAnswer
                    {
                        UserExamAnswerId = _idGenerator.Generate(20),
                        UserExamId = newSession.UserExamId,
                        QuestionId = eq.QuestionBankId,
                        OrderIndex = eq.QuestionNo,
                        SelectedOptionId = null,
                        Question = eq.QuestionBank
                    });
                }
            }

            await _repository.AddSessionAsync(newSession, cancellationToken);
            return OperationResult<UserTakeExamResponse>.Success(MapToResponse(newSession, request.IsShuffle));
        }

        private UserTakeExamResponse MapToResponse(Domain.Entities.UserExam session, bool isShuffleOptions)
        {
            var elapsedSeconds = (int)(DateTime.UtcNow - session.StartTime).TotalSeconds;
            var duration = session.Exam.Duration * 60;
            var remaining = Math.Max(0, duration - elapsedSeconds);

            var questionsMap = new Dictionary<int, QuestionAnswerMetadata>();

            foreach (var a in session.UserExamAnswers)
                questionsMap[a.OrderIndex] = new QuestionAnswerMetadata
                {
                    AnswerId = a.UserExamAnswerId,
                    Question = a.Question,
                    IsWriting = false,
                    SelectedOptionId = a.SelectedOptionId 
                };

            foreach (var w in session.UserExamWritingAnswers)
                questionsMap[w.OrderIndex] = new QuestionAnswerMetadata
                {
                    AnswerId = w.UserExamWritingAnswerId,
                    Question = w.Question,
                    IsWriting = true,
                    AnswerContent = w.AnswerContent 
                };

            var response = new UserTakeExamResponse
            {
                UserExamId = session.UserExamId,
                ExamId = session.ExamId,
                Title = session.Exam.Title,
                Duration = session.Exam.Duration,
                TotalQuestions = questionsMap.Count,
                TimeRemaining = remaining,
                Part = new ExamSkillsDto()
            };

            var parts = session.Exam.ExamTemplate.TemplateParts.OrderBy(p => p.QuestionFrom).ToList();

            response.Part.Listening = parts.Where(p => p.Skill == QuestionSkill.Listening).Select(p => MapToPartDto(p, questionsMap, isShuffleOptions)).ToList();
            response.Part.Reading = parts.Where(p => p.Skill == QuestionSkill.Reading).Select(p => MapToPartDto(p, questionsMap, isShuffleOptions)).ToList();
            response.Part.Writing = parts.Where(p => p.Skill == QuestionSkill.Writing).Select(p => MapToPartDto(p, questionsMap, isShuffleOptions)).ToList();

            return response;
        }

        private ExamPartDto MapToPartDto(TemplatePart part, Dictionary<int, QuestionAnswerMetadata> questionsMap, bool shuffleOptions)
        {
            var questionsDto = new List<ExamQuestionDto>();

            for (int i = part.QuestionFrom; i <= part.QuestionTo; i++)
            {
                if (questionsMap.TryGetValue(i, out var item))
                {
                    var q = item.Question;
                    var options = !item.IsWriting
                        ? q.QuestionOptions.Select(o => new ExamOptionDto
                        {
                            OptionId = o.OptionId,
                            KeyOption = o.KeyOption,
                            Content = o.Content,
                            ImageUrl = o.ImageUrl
                        }).ToList()
                        : new List<ExamOptionDto>();

                    if (shuffleOptions && !item.IsWriting && options.Count > 1)
                    {
                        options = options.OrderBy(x => Guid.NewGuid()).ToList();
                    }

                    questionsDto.Add(new ExamQuestionDto
                    {
                        QuestionId = q.QuestionBankId,
                        QuestionNo = i,
                        Content = q.Content,
                        MediaUrl = q.MediaUrl,
                        MediaType = GetMediaType(q.MediaUrl),
                        PassageContent = q.Passage?.Content,
                        Options = options,
                        SelectedOptionId = item.SelectedOptionId,
                        AnswerContent = item.AnswerContent
                    });
                }
            }

            return new ExamPartDto
            {
                PartId = part.TemplatePartId,
                PartName = part.PartTitle,
                Description = part.Instruction ?? string.Empty,
                ExampleUrl = part.ExampleUrl,
                Questions = questionsDto
            };
        }
        private string GetMediaType(string? url)
        {
            if (string.IsNullOrEmpty(url)) return "None";
            var ext = Path.GetExtension(url).ToLower();
            if (new[] { ".mp3", ".wav", ".ogg", ".m4a" }.Contains(ext)) return "Audio";
            if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(ext)) return "Image";
            return "Unknown";
        }

        private class QuestionAnswerMetadata
        {
            public string AnswerId { get; set; } = string.Empty;
            public QuestionBank Question { get; set; } = null!;
            public bool IsWriting { get; set; }
            public string? SelectedOptionId { get; set; }
            public string? AnswerContent { get; set; }
        }
    }
}