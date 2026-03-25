using MediatR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.UserExam.Queries.GetInProgressExam
{
    public class GetInProgressExamQueryHandler : IRequestHandler<GetInProgressExamQuery, OperationResult<UserTakeExamResponse>>
    {
        private readonly IUserExamRepository _repository;

        public GetInProgressExamQueryHandler(IUserExamRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<UserTakeExamResponse>> Handle(GetInProgressExamQuery request, CancellationToken token)
        {
            var session = await _repository.GetByIdAsync(request.UserExamId, token);

            if (session == null)
                return OperationResult<UserTakeExamResponse>.Failure("Không tìm thấy phiên làm bài đang diễn ra", 404);

            // Bỏ qua việc query Passage lần nữa vì đã được Include trong Repo rồi
            return OperationResult<UserTakeExamResponse>.Success(MapToResponse(session, false));
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

            var parts = session.Exam.ExamTemplate.TemplateParts.OrderBy(p => p.QuestionFrom).ToList();
            var skillSequence = parts.Select(p => p.Skill.ToString()).Distinct().ToList();

            var skillRemaining = new Dictionary<string, int>();
            int currentElapsedSeconds = elapsedSeconds;

            foreach (var s in skillSequence)
            {
                int skillAllocatedSec = session.Exam.SkillDurationsDict.TryGetValue(s, out int d) ? d * 60 : 0;

                int remainingForSkill = Math.Max(0, skillAllocatedSec - currentElapsedSeconds);
                skillRemaining[s] = remainingForSkill;

                currentElapsedSeconds = Math.Max(0, currentElapsedSeconds - skillAllocatedSec);
            }

            var response = new UserTakeExamResponse
            {
                UserExamId = session.UserExamId,
                ExamId = session.ExamId,
                Title = session.Exam.Title,
                Duration = session.Exam.Duration,
                TotalQuestions = questionsMap.Count,
                TimeRemaining = remaining,
                SkillDurations = session.Exam.SkillDurationsDict,
                SkillTimeRemaining = skillRemaining,
                Part = new ExamSkillsDto()
            };

            response.Part.Listening = parts.Where(p => p.Skill == QuestionSkill.Listening)
                                           .Select(p => MapToPartDto(p, questionsMap, isShuffleOptions)).ToList();

            response.Part.Reading = parts.Where(p => p.Skill == QuestionSkill.Reading)
                                         .Select(p => MapToPartDto(p, questionsMap, isShuffleOptions)).ToList();

            response.Part.Writing = parts.Where(p => p.Skill == QuestionSkill.Writing)
                                         .Select(p => MapToPartWritingDto(p, questionsMap)).ToList();

            return response;
        }

        private ExamPartDto MapToPartDto(TemplatePart part, Dictionary<int, QuestionAnswerMetadata> questionsMap, bool shuffleOptions)
        {
            var groups = new List<QuestionGroupDto>();
            QuestionGroupDto? currentGroup = null;

            for (int i = part.QuestionFrom; i <= part.QuestionTo; i++)
            {
                if (questionsMap.TryGetValue(i, out var item) && !item.IsWriting)
                {
                    var q = item.Question;
                    var passage = q.Passage;

                    var options = q.QuestionOptions.Select(o => new ExamOptionDto
                    {
                        OptionId = o.OptionId,
                        KeyOption = o.KeyOption,
                        Content = o.Content,
                        ImageUrl = o.ImageUrl
                    }).ToList();

                    if (shuffleOptions && options.Count > 1)
                        options = options.OrderBy(x => Guid.NewGuid()).ToList();

                    var questionDto = new ExamQuestionDto
                    {
                        UserQuestionId = item.AnswerId,
                        QuestionNo = i,
                        Content = q.Content,
                        SelectedOptionId = item.SelectedOptionId,
                        Options = options
                    };

                    bool isSameGroup = currentGroup != null &&
                                       currentGroup.SharedMediaUrl == q.MediaUrl &&
                                       currentGroup.SharedPassageContent == passage?.Content;

                    if (!isSameGroup)
                    {
                        string? passageMedia = !string.IsNullOrEmpty(passage?.AudioUrl)
                                               ? passage.AudioUrl
                                               : passage?.ImageUrl;

                        currentGroup = new QuestionGroupDto
                        {
                            SharedMediaUrl = q.MediaUrl,
                            SharedMediaType = GetMediaType(q.MediaUrl),
                            SharedPassageContent = passage?.Content,
                            SharedPassageMediaUrl = passageMedia,
                            Questions = new List<ExamQuestionDto>()
                        };
                        groups.Add(currentGroup);
                    }

                    currentGroup.Questions.Add(questionDto);
                }
            }

            return new ExamPartDto
            {
                PartId = part.TemplatePartId,
                PartName = part.PartTitle,
                Description = part.Instruction ?? string.Empty,
                ExampleUrl = part.ExampleUrl,
                QuestionGroups = groups
            };
        }

        private ExamPartWritingDto MapToPartWritingDto(TemplatePart part, Dictionary<int, QuestionAnswerMetadata> questionsMap)
        {
            var groups = new List<QuestionWritingGroupDto>();
            QuestionWritingGroupDto? currentGroup = null;

            for (int i = part.QuestionFrom; i <= part.QuestionTo; i++)
            {
                if (questionsMap.TryGetValue(i, out var item) && item.IsWriting)
                {
                    var q = item.Question;
                    var passage = q.Passage;

                    var questionDto = new ExamQuestionWritingDto
                    {
                        UserQuestionId = item.AnswerId,
                        QuestionNo = i,
                        Content = q.Content,
                        AnswerContent = item.AnswerContent,
                        QuestionTypeCode = q.QuestionType?.Code
                    };

                    bool isSameGroup = currentGroup != null &&
                                       currentGroup.SharedMediaUrl == q.MediaUrl &&
                                       currentGroup.SharedPassageContent == passage?.Content;

                    if (!isSameGroup)
                    {
                        string? passageMedia = !string.IsNullOrEmpty(passage?.AudioUrl)
                                               ? passage.AudioUrl
                                               : passage?.ImageUrl;

                        currentGroup = new QuestionWritingGroupDto
                        {
                            SharedMediaUrl = q.MediaUrl,
                            SharedMediaType = GetMediaType(q.MediaUrl),
                            SharedPassageContent = passage?.Content,
                            SharedPassageMediaUrl = passageMedia,
                            Questions = new List<ExamQuestionWritingDto>()
                        };
                        groups.Add(currentGroup);
                    }

                    currentGroup.Questions.Add(questionDto);
                }
            }

            return new ExamPartWritingDto
            {
                PartId = part.TemplatePartId,
                PartName = part.PartTitle,
                Description = part.Instruction ?? string.Empty,
                ExampleUrl = part.ExampleUrl,
                QuestionGroups = groups
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