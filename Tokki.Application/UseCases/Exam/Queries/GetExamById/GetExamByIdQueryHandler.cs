
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.DTOs;
using Tokki.Application.UseCases.QuestionBanks.DTOs;

namespace Tokki.Application.UseCases.Exam.Queries.GetExamById
{
    public class GetExamByIdQueryHandler : IRequestHandler<GetExamByIdQuery, OperationResult<ExamDto>>
    {
        private readonly IExamRepository _examRepository;

        public GetExamByIdQueryHandler(IExamRepository examRepository)
        {
            _examRepository = examRepository;
        }

        public async Task<OperationResult<ExamDto>> Handle(
            GetExamByIdQuery request,
            CancellationToken cancellationToken)
        {
            var exam = await _examRepository.GetByIdWithDetailsAsync(request.ExamId, cancellationToken);

            if (exam == null)
            {
                return OperationResult<ExamDto>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { AppErrors.ExamNotFound },
                    404,
                    AppErrors.ExamNotFound.Description
                );
            }

            var totalQuestions = exam.ExamTemplate.TemplateParts.Any()
                ? exam.ExamTemplate.TemplateParts.Max(tp => tp.QuestionTo)
                : 0;

            var dto = new ExamDto
            {
                ExamId = exam.ExamId,
                ExamTemplateId = exam.ExamTemplateId,
                ExamTemplateName = exam.ExamTemplate.Name,
                Title = exam.Title,
                Type = exam.Type,
                Status = exam.Status,
                CreatedAt = exam.CreatedAt,
                TotalQuestions = totalQuestions,
                CompletedQuestions = exam.ExamQuestions.Count,
                Questions = exam.ExamQuestions.Select(eq => new ExamQuestionDto
                {
                    ExamQuestionId = eq.ExamQuestionId,
                    QuestionNo = eq.QuestionNo,
                    Score = eq.Score,
                    QuestionBankId = eq.QuestionBankId,
                    QuestionBank = new QuestionBankDto
                    {
                        QuestionBankId = eq.QuestionBank.QuestionBankId,
                        PassageId = eq.QuestionBank.PassageId,
                        PassageTitle = eq.QuestionBank.Passage?.Title,
                        QuestionTypeId = eq.QuestionBank.QuestionTypeId,
                        QuestionTypeName = eq.QuestionBank.QuestionType?.Name,
                        Skill = eq.QuestionBank.Skill,
                        Content = eq.QuestionBank.Content,
                        MediaUrl = eq.QuestionBank.MediaUrl,
                        Explanation = eq.QuestionBank.Explanation,
                        DifficultyLevel = eq.QuestionBank.DifficultyLevel,
                        IsActive = eq.QuestionBank.IsActive,
                        Options = eq.QuestionBank.QuestionOptions.Select(o => new QuestionOptionDto
                        {
                            OptionId = o.OptionId,
                            KeyOption = o.KeyOption,
                            Content = o.Content,
                            ImageUrl = o.ImageUrl,
                            IsCorrect = o.IsCorrect
                        }).OrderBy(o => o.KeyOption).ToList()
                    }
                }).OrderBy(q => q.QuestionNo).ToList()
            };

            return OperationResult<ExamDto>.Success(
                dto,
                200,
                "Lấy thông tin bài test thành công"
            );
        }
    }
}
