using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.TemplateParts.Commands.DeleteTemplatePart;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.TemplateParts
{
    public class DeleteTemplatePartCommandHandlerTests
    {
        private static Mock<ITemplatePartRepository> GetRepoMock(TemplatePart? part = null)
        {
            var m = new Mock<ITemplatePartRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(part);
            m.Setup(x => x.DeleteAsync(It.IsAny<TemplatePart>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            return m;
        }

        private static DeleteTemplatePartCommandHandler CreateHandler(Mock<ITemplatePartRepository>? repo = null)
            => new DeleteTemplatePartCommandHandler(
                (repo ?? GetRepoMock()).Object,
                NullLogger<DeleteTemplatePartCommandHandler>.Instance);

        private static TemplatePart SamplePart(string id = "TP-001") => new TemplatePart
        {
            TemplatePartId = id,
            ExamTemplateId = "T1",
            Skill          = QuestionSkill.Listening,
            QuestionFrom   = 1,
            QuestionTo     = 10,
            PartTitle      = "Part 1",
            Mark           = 2
        };

        // DeleteTemplatePart_01 | A | Part not found → failure
        [Fact]
        public async Task Handle_PartNotFound_ShouldReturnFailure()
        {
            var repo   = GetRepoMock(null);
            var result = await CreateHandler(repo).Handle(new DeleteTemplatePartCommand("MISSING"), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            QACollector.LogTestCase("TemplatePart - Delete", new TestCaseDetail { FunctionGroup = "DeleteTemplatePart", TestCaseID = "DeleteTemplatePart_01", Description = "Part not found → failure (TemplatePartNotFound)", ExpectedResult = "IsSuccess=false", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // DeleteTemplatePart_02 | N | Happy path: part found → 200 with ID
        [Fact]
        public async Task Handle_PartFound_ShouldReturn200WithId()
        {
            var repo   = GetRepoMock(SamplePart("TP-001"));
            var result = await CreateHandler(repo).Handle(new DeleteTemplatePartCommand("TP-001"), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be("TP-001");
            QACollector.LogTestCase("TemplatePart - Delete", new TestCaseDetail { FunctionGroup = "DeleteTemplatePart", TestCaseID = "DeleteTemplatePart_02", Description = "Part found → 200, Data='TP-001'", ExpectedResult = "IsSuccess=true, 200, Data='TP-001'", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Part exists and deleted" } });
        }

        // DeleteTemplatePart_03 | B | DeleteAsync and SaveChangesAsync called once on success
        [Fact]
        public async Task Handle_PartFound_DeleteAndSaveBothCalledOnce()
        {
            var repo = GetRepoMock(SamplePart());
            await CreateHandler(repo).Handle(new DeleteTemplatePartCommand("TP-001"), CancellationToken.None);
            repo.Verify(x => x.DeleteAsync(It.IsAny<TemplatePart>()), Times.Once);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("TemplatePart - Delete", new TestCaseDetail { FunctionGroup = "DeleteTemplatePart", TestCaseID = "DeleteTemplatePart_03", Description = "DeleteAsync and SaveChangesAsync both called once", ExpectedResult = "Both Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Persist calls verified" } });
        }

        // DeleteTemplatePart_04 | B | Part not found → DeleteAsync never called
        [Fact]
        public async Task Handle_PartNotFound_DeleteNeverCalled()
        {
            var repo = GetRepoMock(null);
            await CreateHandler(repo).Handle(new DeleteTemplatePartCommand("MISSING"), CancellationToken.None);
            repo.Verify(x => x.DeleteAsync(It.IsAny<TemplatePart>()), Times.Never);
            QACollector.LogTestCase("TemplatePart - Delete", new TestCaseDetail { FunctionGroup = "DeleteTemplatePart", TestCaseID = "DeleteTemplatePart_04", Description = "Part not found → early return, DeleteAsync never called", ExpectedResult = "Times.Never", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Guard returns before delete" } });
        }

        // DeleteTemplatePart_05 | N | Returned ID equals requested TemplatePartId
        [Fact]
        public async Task Handle_PartFound_ReturnedIdMatchesRequest()
        {
            var repo   = GetRepoMock(SamplePart("TP-XYZ"));
            var result = await CreateHandler(repo).Handle(new DeleteTemplatePartCommand("TP-XYZ"), CancellationToken.None);
            result.Data.Should().Be("TP-XYZ");
            QACollector.LogTestCase("TemplatePart - Delete", new TestCaseDetail { FunctionGroup = "DeleteTemplatePart", TestCaseID = "DeleteTemplatePart_05", Description = "Returned ID='TP-XYZ' matches request", ExpectedResult = "Data='TP-XYZ'", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "ID echoed in response" } });
        }

        // DeleteTemplatePart_06 | A | Repository throws → failure returned
        [Fact]
        public async Task Handle_RepoThrowsOnDelete_ShouldReturnServerError()
        {
            var repo = new Mock<ITemplatePartRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SamplePart());
            repo.Setup(x => x.DeleteAsync(It.IsAny<TemplatePart>())).ThrowsAsync(new Exception("DB error"));
            var result = await CreateHandler(repo).Handle(new DeleteTemplatePartCommand("TP-001"), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            QACollector.LogTestCase("TemplatePart - Delete", new TestCaseDetail { FunctionGroup = "DeleteTemplatePart", TestCaseID = "DeleteTemplatePart_06", Description = "DeleteAsync throws → failure (ServerError)", ExpectedResult = "IsSuccess=false", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Exception caught in try-catch" } });
        }
    }
}
