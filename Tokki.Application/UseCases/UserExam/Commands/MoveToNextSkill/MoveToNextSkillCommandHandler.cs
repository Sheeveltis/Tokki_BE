using MediatR;
using System.Text.Json;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.UserExam.Commands.MoveToNextSkill
{
    public class MoveToNextSkillCommandHandler : IRequestHandler<MoveToNextSkillCommand, OperationResult<bool>>
    {
        private readonly IUserExamRepository _repository;

        public MoveToNextSkillCommandHandler(IUserExamRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<bool>> Handle(MoveToNextSkillCommand request, CancellationToken cancellationToken)
        {
            var userExam = await _repository.GetByIdAsync(request.UserExamId, cancellationToken);
            if (userExam == null)
            {
                return OperationResult<bool>.Failure("Không tìm thấy phiên thi.", 404);
            }

            if (userExam.Status != UserExamStatus.InProgress)
            {
                return OperationResult<bool>.Failure("Phiên thi đã kết thúc.", 400);
            }

            // Get all unique skills in order
            var skillsInOrder = userExam.Exam.ExamTemplate.TemplateParts
                .OrderBy(tp => tp.QuestionFrom)
                .Select(tp => tp.Skill)
                .Distinct()
                .ToList();

            var currentIndex = skillsInOrder.IndexOf(userExam.CurrentSkill);
            if (currentIndex < skillsInOrder.Count - 1)
            {
                // Deserialize FinishedSkills
                var finishedList = string.IsNullOrEmpty(userExam.FinishedSkills) 
                    ? new List<string>() 
                    : JsonSerializer.Deserialize<List<string>>(userExam.FinishedSkills) ?? new List<string>();

                // Add current to finished
                if (!finishedList.Contains(userExam.CurrentSkill.ToString()))
                {
                    finishedList.Add(userExam.CurrentSkill.ToString());
                }
                userExam.FinishedSkills = JsonSerializer.Serialize(finishedList);

                // Move to next
                userExam.CurrentSkill = skillsInOrder[currentIndex + 1];
                userExam.CurrentSkillStartTime = DateTime.UtcNow;
            }
            else
            {
                return OperationResult<bool>.Failure("Bạn đang ở phần thi cuối cùng.", 400);
            }

            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true, 200, $"Đã chuyển sang phần thi {userExam.CurrentSkill}");
        }
    }
}
