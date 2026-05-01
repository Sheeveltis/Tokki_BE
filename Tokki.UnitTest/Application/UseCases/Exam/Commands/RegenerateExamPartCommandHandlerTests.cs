using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.Commands.RegenerateExamPart;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam.Commands
{
    public class RegenerateExamPartCommandHandlerTests
    {
        private readonly Mock<IExamRepository> _examRepoMock = new();
        private readonly Mock<ITemplatePartRepository> _partRepoMock = new();
        private readonly Mock<IExamQuestionRepository> _examQuestionRepoMock = new();
        private readonly Mock<IQuestionBankRepository> _qbRepoMock = new();

        private RegenerateExamPartCommandHandler CreateHandler()
        {
            return new RegenerateExamPartCommandHandler(
                _examRepoMock.Object, _partRepoMock.Object, _examQuestionRepoMock.Object, _qbRepoMock.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // RegenerateExamPartCommandHandler_01 | A | Template Part Missing -> Failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TemplatePartMissing_ShouldReturnFalse()
        {
            _partRepoMock.Setup(x => x.GetByIdAsync("fake", It.IsAny<CancellationToken>())).ReturnsAsync((TemplatePart?)null);
            var handler = CreateHandler();
            var result = await handler.Handle(new RegenerateExamPartCommand { TemplatePartId = "fake" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Không tìm thấy thông tin phần thi mẫu");

            QACollector.LogTestCase("Exam - Regenerate Part", new TestCaseDetail
            {
                FunctionGroup = "RegenerateExamPartCommandHandler",
                TestCaseID = "RegenerateExamPartCommandHandler_01",
                Description = "Checks part existence to safely break if missing",
                ExpectedResult = "Return false error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TemplatePart = null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // RegenerateExamPartCommandHandler_02 | A | Exam Missing -> Failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExamMissing_ShouldReturnFalse()
        {
            _partRepoMock.Setup(x => x.GetByIdAsync("tp1", It.IsAny<CancellationToken>())).ReturnsAsync(new TemplatePart());
            _examRepoMock.Setup(x => x.GetByIdAsync("e1", It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Exam?)null);
            var handler = CreateHandler();
            var result = await handler.Handle(new RegenerateExamPartCommand { TemplatePartId = "tp1", ExamId = "e1" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Không tìm thấy đề thi");

            QACollector.LogTestCase("Exam - Regenerate Part", new TestCaseDetail
            {
                FunctionGroup = "RegenerateExamPartCommandHandler",
                TestCaseID = "RegenerateExamPartCommandHandler_02",
                Description = "Ensures Exam object exists before trying to modify it",
                ExpectedResult = "Return false error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Exam = null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // RegenerateExamPartCommandHandler_03 | A | ExamTemplateId Mismatch -> Failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExamTemplateIdMismatch_ShouldReturnFalse()
        {
            _partRepoMock.Setup(x => x.GetByIdAsync("tp1", It.IsAny<CancellationToken>())).ReturnsAsync(new TemplatePart { ExamTemplateId = "t1" });
            _examRepoMock.Setup(x => x.GetByIdAsync("e1", It.IsAny<CancellationToken>())).ReturnsAsync(new Domain.Entities.Exam { ExamTemplateId = "t2" });
            
            var handler = CreateHandler();
            var result = await handler.Handle(new RegenerateExamPartCommand { TemplatePartId = "tp1", ExamId = "e1" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Phần thi này không khớp với cấu trúc đề thi");

            QACollector.LogTestCase("Exam - Regenerate Part", new TestCaseDetail
            {
                FunctionGroup = "RegenerateExamPartCommandHandler",
                TestCaseID = "RegenerateExamPartCommandHandler_03",
                Description = "Security matching to ensure no part from a different template is injected",
                ExpectedResult = "Return false mismatch",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Exam.ExamTemplateId != Part.ExamTemplateId" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // RegenerateExamPartCommandHandler_04 | A | Validation Error Questions <= 0
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ZeroQuantity_ShouldReturnFalse()
        {
            _partRepoMock.Setup(x => x.GetByIdAsync("tp1", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new TemplatePart { ExamTemplateId = "t1", QuestionFrom = 2, QuestionTo = 1 }); // Leads to quantity = 0
            _examRepoMock.Setup(x => x.GetByIdAsync("e1", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new Domain.Entities.Exam { ExamTemplateId = "t1" });
            
            var handler = CreateHandler();
            var result = await handler.Handle(new RegenerateExamPartCommand { TemplatePartId = "tp1", ExamId = "e1" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Cấu hình số lượng câu hỏi không hợp lệ");

            QACollector.LogTestCase("Exam - Regenerate Part", new TestCaseDetail
            {
                FunctionGroup = "RegenerateExamPartCommandHandler",
                TestCaseID = "RegenerateExamPartCommandHandler_04",
                Description = "Zero or negative questions check blocks operation",
                ExpectedResult = "Return false error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "quantityNeeded <= 0" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // RegenerateExamPartCommandHandler_05 | A | Not Enough Questions in DB -> Failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NotEnoughBankQuestions_ShouldReturnFalse()
        {
            _partRepoMock.Setup(x => x.GetByIdAsync("tp1", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new TemplatePart { ExamTemplateId = "t1", QuestionFrom = 1, QuestionTo = 2, QuestionTypeId = "qt1" }); // Needs 2 correctly
            _examRepoMock.Setup(x => x.GetByIdAsync("e1", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new Domain.Entities.Exam { ExamTemplateId = "t1" });
            _examQuestionRepoMock.Setup(x => x.GetByExamIdAsync("e1", It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(new List<ExamQuestion>()); // Mock current empty questions array
            
            // Returns only 1 from repository, despite needing 2
            _qbRepoMock.Setup(x => x.GetRandomQuestionsByTypeAsync("qt1", 2, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<QuestionBank> { new QuestionBank { QuestionBankId = "q1" } });

            var handler = CreateHandler();
            var result = await handler.Handle(new RegenerateExamPartCommand { TemplatePartId = "tp1", ExamId = "e1" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Kho câu hỏi không đủ");

            QACollector.LogTestCase("Exam - Regenerate Part", new TestCaseDetail
            {
                FunctionGroup = "RegenerateExamPartCommandHandler",
                TestCaseID = "RegenerateExamPartCommandHandler_05",
                Description = "System fails safely if QuestionBank repo doesn't contain sufficient items",
                ExpectedResult = "Return false insufficient items",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Available < Needed" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // RegenerateExamPartCommandHandler_06 | N | Success -> Remove Old and Push New
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRandomization_ShouldSwapAndSaveCorrectly()
        {
             _partRepoMock.Setup(x => x.GetByIdAsync("tp1", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new TemplatePart { ExamTemplateId = "t1", QuestionFrom = 1, QuestionTo = 1, QuestionTypeId = "qt1" }); // Needs 1
            _examRepoMock.Setup(x => x.GetByIdAsync("e1", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new Domain.Entities.Exam { ExamTemplateId = "t1" });
            
            var oldQ = new ExamQuestion { QuestionNo = 1, QuestionBankId = "q-old" };
            var notAffectedQ = new ExamQuestion { QuestionNo = 2, QuestionBankId = "q-ok" };
            
            _examQuestionRepoMock.Setup(x => x.GetByExamIdAsync("e1", It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(new List<ExamQuestion> { oldQ, notAffectedQ }); 
            
            _qbRepoMock.Setup(x => x.GetRandomQuestionsByTypeAsync("qt1", 1, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<QuestionBank> { new QuestionBank { QuestionBankId = "q-new" } });

            var handler = CreateHandler();
            var result = await handler.Handle(new RegenerateExamPartCommand { TemplatePartId = "tp1", ExamId = "e1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            
            _examQuestionRepoMock.Verify(x => x.DeleteRangeAsync(It.Is<List<ExamQuestion>>(list => list.Contains(oldQ) && list.Count == 1)), Times.Once);
            _examQuestionRepoMock.Verify(x => x.AddRangeAsync(It.Is<List<ExamQuestion>>(list => list[0].QuestionBankId == "q-new")), Times.Once);

            QACollector.LogTestCase("Exam - Regenerate Part", new TestCaseDetail
            {
                FunctionGroup = "RegenerateExamPartCommandHandler",
                TestCaseID = "RegenerateExamPartCommandHandler_06",
                Description = "Valid configuration removes old matching questions and inserts exactly requested amount new questions",
                ExpectedResult = "Return Success True",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Returns exactly as expected" }
            });
        }
    }
}
