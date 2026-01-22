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

namespace Tokki.Application.UseCases.Exam.Queries.GetExamDetailQuery
{
    public class GetExamDetailQueryHandler : IRequestHandler<GetExamDetailQuery, OperationResult<ExamDetailDto>>
    {
        private readonly IExamRepository _examRepo;
        private readonly ITemplatePartRepository _partRepo;

        public GetExamDetailQueryHandler(IExamRepository examRepo, ITemplatePartRepository partRepo)
        {
            _examRepo = examRepo;
            _partRepo = partRepo;
        }

        public async Task<OperationResult<ExamDetailDto>> Handle(GetExamDetailQuery request, CancellationToken cancellationToken)
        {
            var exam = await _examRepo.GetExamWithFullDetailsAsync(request.ExamId, cancellationToken);

            if (exam == null)
            {
                return OperationResult<ExamDetailDto>.Failure("Không tìm thấy đề thi.", 404);
            }

            var parts = await _partRepo.GetByExamTemplateIdAsync(exam.ExamTemplateId, cancellationToken);

            var sortedParts = parts.OrderBy(p => p.QuestionFrom).ToList();

            var result = new ExamDetailDto
            {
                ExamId = exam.ExamId,
                Title = exam.Title,
                Duration = exam.Duration,
                Type = exam.Type,
                Status = exam.Status, 
                CreatedAt = exam.CreatedAt,
                TemplateParts = new List<ExamPartDto>()
            };

            foreach (var part in sortedParts)
            {
                var partDto = new ExamPartDto
                {
                    TemplatePartsTitle = $"[{part.QuestionFrom}~{part.QuestionTo}] {part.Instruction} (각 {part.Mark} 점)",
                    ExampleUrl = part.ExampleUrl,
                    Questions = new List<ExamQuestionDetailDto>()
                };

                var questionsInPart = exam.ExamQuestions
                    .Where(eq => eq.QuestionNo >= part.QuestionFrom && eq.QuestionNo <= part.QuestionTo)
                    .OrderBy(eq => eq.QuestionNo) 
                    .ToList();

                foreach (var eq in questionsInPart)
                {
                    var qBank = eq.QuestionBank;
                    if (qBank == null) continue;

                    string mediaType = MapSkillToMediaType(qBank.QuestionType?.Skill);

                    var questionDto = new ExamQuestionDetailDto
                    {
                        QuestionNo = eq.QuestionNo,
                        Content = qBank.Content,
                        Explanation = qBank.Explanation,
                        MediaUrl = qBank.MediaUrl,
                        MediaType = mediaType,

                        PassageContent = qBank.Passage?.Content,
                        PassageImageUrl = qBank.Passage?.ImageUrl,
                        PassageAudioUrl = qBank.Passage?.AudioUrl,
                        PassageMediaType = qBank.Passage != null ? qBank.Passage.MediaType.ToString() : null,

                        Options = qBank.QuestionOptions.Select(opt => new QuestionOptionDto
                        {
                            Content = opt.Content,
                            ImageUrl = opt.ImageUrl,
                            IsCorrect = opt.IsCorrect,
                            KeyOption = opt.KeyOption
                        }).OrderBy(o => o.KeyOption).ToList()
                    };

                    partDto.Questions.Add(questionDto);
                }

                result.TemplateParts.Add(partDto);
            }

            return OperationResult<ExamDetailDto>.Success(result);
        }

        private string MapSkillToMediaType(QuestionSkill? skill)
        {
            if (!skill.HasValue) return "Image";

            return skill.Value switch
            {
                QuestionSkill.Listening => "Audio",
                QuestionSkill.Reading => "Image",
                QuestionSkill.Writing => "Image",
                _ => "Image"
            };
        }
    }
}
