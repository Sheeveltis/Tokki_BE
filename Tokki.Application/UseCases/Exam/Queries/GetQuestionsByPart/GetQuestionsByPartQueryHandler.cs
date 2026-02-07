using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.Queries.GetQuestionsByPart
{
    public class GetQuestionsByPartQueryHandler : IRequestHandler<GetQuestionsByPartQuery, OperationResult<PagedResult<AvailableQuestionDTO>>>
    {
        private readonly ITemplatePartRepository _templatePartRepository;
        private readonly IQuestionBankRepository _questionBankRepository;

        public GetQuestionsByPartQueryHandler(
            ITemplatePartRepository templatePartRepository,
            IQuestionBankRepository questionBankRepository)
        {
            _templatePartRepository = templatePartRepository;
            _questionBankRepository = questionBankRepository;
        }

        public async Task<OperationResult<PagedResult<AvailableQuestionDTO>>> Handle(GetQuestionsByPartQuery request, CancellationToken cancellationToken)
        {
            var part = await _templatePartRepository.GetByIdAsync(request.TemplatePartId, cancellationToken);
            if (part == null)
                return OperationResult<PagedResult<AvailableQuestionDTO>>.Failure(AppErrors.TemplatePartNotFound, 404);

            var (items, totalCount) = await _questionBankRepository.GetAvailableQuestionsByTypeAsync(
                part.QuestionTypeId,
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                cancellationToken
            );

            var dtos = items.Select(q => new AvailableQuestionDTO
            {
                QuestionBankId = q.QuestionBankId,
                Content = q.Content,
                Explanation = q.Explanation,
                MediaUrl = q.MediaUrl,
                MediaType = MapSkillToMediaType(q.QuestionType?.Skill),

                PassageContent = q.Passage?.Content,
                PassageImageUrl = q.Passage?.ImageUrl,
                PassageAudioUrl = q.Passage?.AudioUrl,
                PassageMediaType = q.Passage != null ? q.Passage.MediaType.ToString() : null,

                Options = q.QuestionOptions.Select(opt => new QuestionOptionDto
                {
                    KeyOption = opt.KeyOption,
                    Content = opt.Content,
                    ImageUrl = opt.ImageUrl,
                    IsCorrect = opt.IsCorrect
                })
                .OrderBy(o => o.KeyOption)
                .ToList()

            }).ToList();

            var pagedResult = PagedResult<AvailableQuestionDTO>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<AvailableQuestionDTO>>.Success(pagedResult);
        }

        private string MapSkillToMediaType(QuestionSkill? skill)
        {
            if (!skill.HasValue) return "Image";
            return skill.Value == QuestionSkill.Listening ? "Audio" : "Image";
        }
    }
}
