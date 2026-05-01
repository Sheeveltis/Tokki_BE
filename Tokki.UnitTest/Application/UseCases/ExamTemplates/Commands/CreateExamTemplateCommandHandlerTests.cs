using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.ExamTemplates.Commands.CreateExamTemplate;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.ExamTemplates.Commands
{
    public class CreateExamTemplateCommandHandlerTests
    {
        private readonly Mock<IExamTemplateRepository> _repoMock = new();
        private readonly Mock<IIdGeneratorService> _idServiceMock = new();
        private readonly Mock<IHttpContextAccessor> _httpMock = new();

        private CreateExamTemplateCommandHandler CreateHandler()
        {
            return new CreateExamTemplateCommandHandler(_repoMock.Object, _idServiceMock.Object, _httpMock.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // CreateExamTemplateCommandHandler_01 | A | Name Conflicted Duplicate
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NameConflicted_ShouldReturnFailure()
        {
            _repoMock.Setup(x => x.IsNameExistsAsync("dup", null)).ReturnsAsync(true);
            var handler = CreateHandler();
            var result = await handler.Handle(new CreateExamTemplateCommand { Name = "dup" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Tên đề thi mẫu đã tồn tại");

            QACollector.LogTestCase("Exam Template - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateExamTemplateCommandHandler",
                TestCaseID = "CreateExamTemplateCommandHandler_01",
                Description = "Rejects same identical name validation securely avoiding SQL errors",
                ExpectedResult = "Return Duplication error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DB contains same name validation" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateExamTemplateCommandHandler_02 | N | Success -> Fallback CreatedBy = SYSTEM
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MissingCreatedBy_ShouldFallbackToSystem()
        {
            _repoMock.Setup(x => x.IsNameExistsAsync("ok", null)).ReturnsAsync(false);
            _idServiceMock.Setup(x => x.GenerateCustom(10)).Returns("gen-id");
            _httpMock.Setup(x => x.HttpContext).Returns((HttpContext?)null); // Completely empty HTTP

            var handler = CreateHandler();
            var result = await handler.Handle(new CreateExamTemplateCommand { Name = "ok", CreatedBy = null }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("gen-id");

            _repoMock.Verify(x => x.AddAsync(It.Is<Domain.Entities.ExamTemplate>(t => t.CreatedBy == "SYSTEM" && t.Status == ExamTemplateStatus.Draft)), Times.Once);

            QACollector.LogTestCase("Exam Template - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateExamTemplateCommandHandler",
                TestCaseID = "CreateExamTemplateCommandHandler_02",
                Description = "Ensures background offline requests default assignment SYSTEM safely",
                ExpectedResult = "Successfully creates with mapped values",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No HTTP Context / CreatedBy null explicitly" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateExamTemplateCommandHandler_03 | N | Success -> CreatedBy maps from Provided String
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CreatedByString_ShouldMapCorrectly()
        {
            _repoMock.Setup(x => x.IsNameExistsAsync("ok", null)).ReturnsAsync(false);
            _idServiceMock.Setup(x => x.GenerateCustom(10)).Returns("gen-id");

            var handler = CreateHandler();
            var result = await handler.Handle(new CreateExamTemplateCommand { Name = "ok", CreatedBy = "usr123" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _repoMock.Verify(x => x.AddAsync(It.Is<Domain.Entities.ExamTemplate>(t => t.CreatedBy == "usr123")), Times.Once);

            QACollector.LogTestCase("Exam Template - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateExamTemplateCommandHandler",
                TestCaseID = "CreateExamTemplateCommandHandler_03",
                Description = "Command payload correctly maps property without HTTP extraction",
                ExpectedResult = "Creates correctly via internal request assignment",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Payload Provided CreatedBy string" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateExamTemplateCommandHandler_04 | N | Success -> CreatedBy HTTP NameIdentifier Claim mapped
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_HTTPNameIdentifier_ShouldExtractClaimSuccessfully()
        {
            _repoMock.Setup(x => x.IsNameExistsAsync("ok", null)).ReturnsAsync(false);
            _idServiceMock.Setup(x => x.GenerateCustom(10)).Returns("gen-id");

            var context = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "claim-id") });
            context.User = new ClaimsPrincipal(identity);
            _httpMock.Setup(x => x.HttpContext).Returns(context);

            var handler = CreateHandler();
            var result = await handler.Handle(new CreateExamTemplateCommand { Name = "ok" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _repoMock.Verify(x => x.AddAsync(It.Is<Domain.Entities.ExamTemplate>(t => t.CreatedBy == "claim-id")), Times.Once);

            QACollector.LogTestCase("Exam Template - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateExamTemplateCommandHandler",
                TestCaseID = "CreateExamTemplateCommandHandler_04",
                Description = "Auth extraction logic perfectly extracts system valid NameIdentifiers efficiently",
                ExpectedResult = "Maps claim correctly",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Principal Claim defined context payload" }
            });
        }
        
        // ═══════════════════════════════════════════════════════════
        // CreateExamTemplateCommandHandler_05 | N | Success -> CreatedBy SUB Claim fallback
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_HTTPSubClaim_ShouldExtractAppropriately()
        {
            _repoMock.Setup(x => x.IsNameExistsAsync("ok", null)).ReturnsAsync(false);
            _idServiceMock.Setup(x => x.GenerateCustom(10)).Returns("gen-id");

            var context = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[] { new Claim("sub", "sub-id") }); // No NameIdentifier, just sub
            context.User = new ClaimsPrincipal(identity);
            _httpMock.Setup(x => x.HttpContext).Returns(context);

            var handler = CreateHandler();
            var result = await handler.Handle(new CreateExamTemplateCommand { Name = "ok" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _repoMock.Verify(x => x.AddAsync(It.Is<Domain.Entities.ExamTemplate>(t => t.CreatedBy == "sub-id")), Times.Once);

            QACollector.LogTestCase("Exam Template - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateExamTemplateCommandHandler",
                TestCaseID = "CreateExamTemplateCommandHandler_05",
                Description = "Different fallback Auth frameworks utilizing SUB mapping also accurately matched",
                ExpectedResult = "Maps sub smoothly",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Uses SUB not NameIdentifier" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateExamTemplateCommandHandler_06 | N | Success -> Sets Status explicitly to Draft overriding defaults safely
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EnsureDraftStatus()
        {
            _repoMock.Setup(x => x.IsNameExistsAsync("ok", null)).ReturnsAsync(false);
            _idServiceMock.Setup(x => x.GenerateCustom(10)).Returns("gen-id");
            var handler = CreateHandler();
            
            var result = await handler.Handle(new CreateExamTemplateCommand { Name = "ok" }, CancellationToken.None);

            _repoMock.Verify(x => x.AddAsync(It.Is<Domain.Entities.ExamTemplate>(t => t.Status == ExamTemplateStatus.Draft)), Times.Once);

            QACollector.LogTestCase("Exam Template - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateExamTemplateCommandHandler",
                TestCaseID = "CreateExamTemplateCommandHandler_06",
                Description = "Verify newly instantiated DB entities strictly initialize securely at Draft state initially",
                ExpectedResult = "Status verified at Draft successfully",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Ensure basic model compliance properties" }
            });
        }
    }
}
