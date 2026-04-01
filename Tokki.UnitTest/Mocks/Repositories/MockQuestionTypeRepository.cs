using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockQuestionTypeRepository
    {
        public static Mock<IQuestionTypeRepository> GetMock(
            QuestionType?              returnedById    = null,
            IEnumerable<QuestionType>? returnedAll     = null,
            bool                       nameExists      = false,
            bool                       codeExists      = false,
            (IEnumerable<QuestionType> items, int totalCount)? pagedResult = null)
        {
            var mock = new Mock<IQuestionTypeRepository>();

            mock.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnedById);

            mock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnedAll ?? new List<QuestionType>());

            mock.Setup(x => x.IsNameExistsAsync(It.IsAny<string>(), It.IsAny<string?>()))
                .ReturnsAsync(nameExists);

            mock.Setup(x => x.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<string?>()))
                .ReturnsAsync(codeExists);

            var paged = pagedResult ?? (new List<QuestionType>(), 0);
            mock.Setup(x => x.GetPagedAsync(
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<string?>(),
                        It.IsAny<QuestionSkill?>(),
                        It.IsAny<DifficultyLevel?>(),
                        It.IsAny<ExamType?>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(paged);

            mock.Setup(x => x.AddAsync(It.IsAny<QuestionType>())).Returns(Task.CompletedTask);
            mock.Setup(x => x.UpdateAsync(It.IsAny<QuestionType>())).Returns(Task.CompletedTask);
            mock.Setup(x => x.DeleteAsync(It.IsAny<QuestionType>())).Returns(Task.CompletedTask);
            mock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            return mock;
        }

        // ── Sample Data ───────────────────────────────────────────
        public static QuestionType GetSampleActive(string id = "QT-001", QuestionSkill skill = QuestionSkill.Reading)
            => new QuestionType
            {
                QuestionTypeId = id,
                Name           = "Reading Basic",
                Code           = "RB001",
                Skill          = skill,
                IsActive       = true
            };

        public static QuestionType GetSampleInactive(string id = "QT-INV-01")
            => new QuestionType
            {
                QuestionTypeId = id,
                Name           = "Inactive Type",
                Code           = "INV001",
                Skill          = QuestionSkill.Writing,
                IsActive       = false
            };

        public static List<QuestionType> GetSampleList(int count = 3)
        {
            var list = new List<QuestionType>();
            for (int i = 1; i <= count; i++)
                list.Add(GetSampleActive($"QT-00{i}"));
            return list;
        }
    }
}
