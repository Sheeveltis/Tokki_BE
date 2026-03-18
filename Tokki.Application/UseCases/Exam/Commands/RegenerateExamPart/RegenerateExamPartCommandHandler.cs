using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.Commands.RegenerateExamPart
{
    public class RegenerateExamPartCommandHandler : IRequestHandler<RegenerateExamPartCommand, OperationResult<bool>>
    {
        private readonly IExamRepository _examRepository;
        private readonly ITemplatePartRepository _templatePartRepository;
        private readonly IExamQuestionRepository _examQuestionRepository;
        private readonly IQuestionBankRepository _questionBankRepository;

        public RegenerateExamPartCommandHandler(
            IExamRepository examRepository,
            ITemplatePartRepository templatePartRepository,
            IExamQuestionRepository examQuestionRepository,
            IQuestionBankRepository questionBankRepository)
        {
            _examRepository = examRepository;
            _templatePartRepository = templatePartRepository;
            _examQuestionRepository = examQuestionRepository;
            _questionBankRepository = questionBankRepository;
        }

        public async Task<OperationResult<bool>> Handle(RegenerateExamPartCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var templatePart = await _templatePartRepository.GetByIdAsync(request.TemplatePartId, cancellationToken);
                if (templatePart == null)
                    return OperationResult<bool>.Failure("Không tìm thấy thông tin phần thi mẫu (Template Part).");

                var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken);
                if (exam == null)
                    return OperationResult<bool>.Failure("Không tìm thấy đề thi.");

                if (exam.ExamTemplateId != templatePart.ExamTemplateId)
                    return OperationResult<bool>.Failure("Phần thi này không khớp với cấu trúc đề thi hiện tại.");

                int quantityNeeded = templatePart.QuestionTo - templatePart.QuestionFrom + 1;
                if (quantityNeeded <= 0)
                    return OperationResult<bool>.Failure("Cấu hình số lượng câu hỏi không hợp lệ.");

                var currentExamQuestions = await _examQuestionRepository.GetByExamIdAsync(request.ExamId, cancellationToken);

                var questionsToRemove = currentExamQuestions
                    .Where(x => x.QuestionNo >= templatePart.QuestionFrom
                             && x.QuestionNo <= templatePart.QuestionTo)
                    .ToList();

                var excludedQuestionBankIds = questionsToRemove
                    .Select(x => x.QuestionBankId)
                    .Distinct()
                    .ToList();

                if (questionsToRemove.Any())
                {
                    await _examQuestionRepository.DeleteRangeAsync(questionsToRemove);
                }

                var newQuestions = await _questionBankRepository.GetRandomQuestionsByTypeAsync(
                    templatePart.QuestionTypeId,
                    quantityNeeded,
                    excludedQuestionBankIds,
                    cancellationToken
                );

                if (newQuestions.Count < quantityNeeded)
                {
                    return OperationResult<bool>.Failure($"Kho câu hỏi không đủ. Cần {quantityNeeded} câu, chỉ tìm thấy {newQuestions.Count} câu mới.");
                }

                var examQuestionsToAdd = new List<ExamQuestion>();
                int currentQuestionNo = templatePart.QuestionFrom;

                foreach (var q in newQuestions)
                {
                    examQuestionsToAdd.Add(new ExamQuestion
                    {
                        ExamQuestionId = Guid.NewGuid().ToString().Substring(0, 10),
                        ExamId = request.ExamId,
                        QuestionBankId = q.QuestionBankId,
                        QuestionNo = currentQuestionNo,
                        Score = templatePart.Mark > 0 ? (templatePart.Mark / quantityNeeded) : 2
                    });
                    currentQuestionNo++;
                }

                await _examQuestionRepository.AddRangeAsync(examQuestionsToAdd);

                await _examQuestionRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                // Log error
                return OperationResult<bool>.Failure($"Lỗi hệ thống: {ex.Message}");
            }
        }
    }
}
