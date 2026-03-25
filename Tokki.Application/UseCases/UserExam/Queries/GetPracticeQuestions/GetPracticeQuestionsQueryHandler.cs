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

namespace Tokki.Application.UseCases.UserExam.Queries.GetPracticeQuestions
{
    public class GetPracticeQuestionsQueryHandler : IRequestHandler<GetPracticeQuestionsQuery, OperationResult<List<QuestionResultGroupDto>>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IPassageRepository _passageRepository;

        public GetPracticeQuestionsQueryHandler(IQuestionBankRepository questionBankRepository, IPassageRepository passageRepository)
        {
            _questionBankRepository = questionBankRepository;
            _passageRepository = passageRepository;
        }

        public async Task<OperationResult<List<QuestionResultGroupDto>>> Handle(GetPracticeQuestionsQuery request, CancellationToken token)
        {
            var questions = await _questionBankRepository.GetRandomQuestionsForPracticeAsync(
                request.QuestionTypeId,
                request.Quantity,
                token);

            if (questions == null || !questions.Any())
                return OperationResult<List<QuestionResultGroupDto>>.Failure("Không tìm thấy câu hỏi phù hợp trong kho.", 404);

            var passageIds = questions.Select(q => q.PassageId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            Dictionary<string, Passage> passageMap = new();
            if (passageIds.Any())
            {
                var passages = await _passageRepository.GetByIdsAsync(passageIds!, token);
                passageMap = passages.ToDictionary(p => p.PassageId);
            }

            var groupsDto = new List<QuestionResultGroupDto>();
            QuestionResultGroupDto? currentGroup = null;
            int questionNo = 1;

            var sortedQuestions = questions.OrderBy(q => q.PassageId).ToList();

            foreach (var q in sortedQuestions)
            {
                Passage? passage = null;
                if (!string.IsNullOrEmpty(q.PassageId)) passageMap.TryGetValue(q.PassageId, out passage);

                var correctOption = q.QuestionOptions.FirstOrDefault(o => o.IsCorrect);

                var questionDto = new QuestionResultDto
                {
                    QuestionNo = questionNo++,
                    Content = q.Content,
                    SelectedOptionId = null,
                    CorrectOptionId = correctOption?.OptionId,
                    Explanation = q.Explanation, // Thêm Explanation vào đây
                    Options = q.QuestionOptions.Select(o => new ExamOptionDto
                    {
                        OptionId = o.OptionId,
                        KeyOption = o.KeyOption,
                        Content = o.Content,
                        ImageUrl = o.ImageUrl
                    }).ToList()
                };

                string? passageMedia = !string.IsNullOrEmpty(passage?.AudioUrl) ? passage.AudioUrl : passage?.ImageUrl;
                string? finalGroupMedia = !string.IsNullOrEmpty(q.MediaUrl) ? q.MediaUrl : passageMedia;

                bool isSameGroup = currentGroup != null &&
                                   currentGroup.SharedMediaUrl == finalGroupMedia &&
                                   currentGroup.SharedPassageContent == passage?.Content;

                if (!isSameGroup)
                {
                    currentGroup = new QuestionResultGroupDto
                    {
                        SharedMediaUrl = finalGroupMedia,
                        SharedMediaType = GetMediaType(finalGroupMedia),
                        SharedPassageContent = passage?.Content,
                        Questions = new List<QuestionResultDto>()
                    };
                    groupsDto.Add(currentGroup);
                }

                currentGroup.Questions.Add(questionDto);
            }

            return OperationResult<List<QuestionResultGroupDto>>.Success(groupsDto);
        }

        private string GetMediaType(string? url)
        {
            if (string.IsNullOrEmpty(url)) return "None";
            var ext = Path.GetExtension(url).ToLower();
            if (new[] { ".mp3", ".wav", ".ogg", ".m4a" }.Contains(ext)) return "Audio";
            if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(ext)) return "Image";
            return "Unknown";
        }
    }
}