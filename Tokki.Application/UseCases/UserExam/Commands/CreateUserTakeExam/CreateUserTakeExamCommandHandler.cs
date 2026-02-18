using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.UserExam.Commands.CreateUserTakeExam
{
    public class CreateUserTakeExamCommandHandler : IRequestHandler<CreateUserTakeExamCommand, OperationResult<CreateUserTakeExamResponse>>
    {
        private readonly IUserExamRepository _repository;
        private readonly IIdGeneratorService _idGenerator;

        public CreateUserTakeExamCommandHandler(IUserExamRepository repository, IIdGeneratorService idGenerator)
        {
            _repository = repository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<CreateUserTakeExamResponse>> Handle(CreateUserTakeExamCommand request, CancellationToken cancellationToken)
        {
            var existingSession = await _repository.GetInProgressSessionAsync(request.UserId, request.ExamId, cancellationToken);
            if (existingSession != null)
            {
                return OperationResult<CreateUserTakeExamResponse>.Success(new CreateUserTakeExamResponse { UserExamId = existingSession.UserExamId });
            }

            var originalExam = await _repository.GetExamWithFullStructureAsync(request.ExamId, cancellationToken);
            if (originalExam == null) return OperationResult<CreateUserTakeExamResponse>.Failure("Không tìm thấy đề thi", 404);

            var newSession = new Domain.Entities.UserExam
            {
                UserExamId = _idGenerator.Generate(15),
                UserId = request.UserId,
                ExamId = originalExam.ExamId,
                StartTime = DateTime.UtcNow,
                Status = UserExamStatus.InProgress,
                Score = 0
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
                        AnswerContent = string.Empty
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
                        SelectedOptionId = null
                    });
                }
            }

            await _repository.AddSessionAsync(newSession, cancellationToken);

            return OperationResult<CreateUserTakeExamResponse>.Success(new CreateUserTakeExamResponse
            {
                UserExamId = newSession.UserExamId
            });
        }
    }
}