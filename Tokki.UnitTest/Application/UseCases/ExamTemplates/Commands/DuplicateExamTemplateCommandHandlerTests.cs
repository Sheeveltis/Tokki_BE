using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.ExamTemplates.Commands.DuplicateExamTemplate;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.ExamTemplates.Commands
{
    public class DuplicateExamTemplateCommandHandlerTests
    {
        private readonly Mock<IExamTemplateRepository> _mockTemplateRepo;
        private readonly Mock<ITemplatePartRepository> _mockPartRepo;
        private readonly Mock<IIdGeneratorService> _mockIdGen;
        private readonly Mock<ILogger<DuplicateExamTemplateCommandHandler>> _mockLogger;
        private readonly Mock<IHttpContextAccessor> _mockHttpContext;
        private readonly DuplicateExamTemplateCommandHandler _handler;

        public DuplicateExamTemplateCommandHandlerTests()
        {
            _mockTemplateRepo = new Mock<IExamTemplateRepository>();
            _mockPartRepo = new Mock<ITemplatePartRepository>();
            _mockIdGen = new Mock<IIdGeneratorService>();
            _mockLogger = new Mock<ILogger<DuplicateExamTemplateCommandHandler>>();
            _mockHttpContext = new Mock<IHttpContextAccessor>();

            _handler = new DuplicateExamTemplateCommandHandler(
                _mockTemplateRepo.Object,
                _mockPartRepo.Object,
                _mockIdGen.Object,
                _mockLogger.Object,
                _mockHttpContext.Object
            );
        }

        private void SetupHttpContext(string userId)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims,"TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            _mockHttpContext.Setup(x => x.HttpContext).Returns(httpContext);
        }

        [Fact]
        public async Task Handle_TemplateNotFound_ReturnsFailure404()
        {
            var command = new DuplicateExamTemplateCommand("T1");

            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync((ExamTemplate?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("M?u d? thi không t?n t?i");

            QACollector.LogTestCase("ExamTemplate - Duplicate", new TestCaseDetail
            {
                FunctionGroup     ="DuplicateExamTemplateCommandHandler",
                TestCaseID        ="DuplicateExamTemplateCommandHandler_01",
                Description       ="Template is",
                ExpectedResult    ="Returns",
                StatusRound1      ="Passed",
                TestCaseType      ="A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Null" }
            });
        }

        [Fact]
        public async Task Handle_SuccessEmptyParts_DuplicatesWithoutParts()
        {
            SetupHttpContext("U1");
            var command = new DuplicateExamTemplateCommand("T1");
            
            var existingTemplate = new ExamTemplate { ExamTemplateId ="T1", Name ="Base", TemplateParts = new List<TemplatePart>() };
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>())).ReturnsAsync(existingTemplate);
            
            _mockIdGen.Setup(x => x.GenerateCustom(10)).Returns("NEW_T1");
            _mockTemplateRepo.Setup(x => x.IsNameExistsAsync("Base (1)", null)).ReturnsAsync(false);
            // Notice: IsNameExistsAsync takes excludeId=null by default, checking mock setup.

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("NEW_T1");
            
            _mockPartRepo.Verify(x => x.AddRangeAsync(It.IsAny<IEnumerable<TemplatePart>>()), Times.Never);
            _mockTemplateRepo.Verify(x => x.AddAsync(It.Is<ExamTemplate>(t => t.Name =="Base (1)" && t.CreatedBy =="U1")), Times.Once);

            QACollector.LogTestCase("ExamTemplate - Duplicate", new TestCaseDetail
            {
                FunctionGroup     ="DuplicateExamTemplateCommandHandler",
                TestCaseID        ="DuplicateExamTemplateCommandHandler_02",
                Description       ="Missing parts skips",
                ExpectedResult    ="Skips",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Empty" }
            });
        }

        [Fact]
        public async Task Handle_SuccessWithParts_DuplicatesPartsWithNewIds()
        {
             SetupHttpContext("U1");
            var command = new DuplicateExamTemplateCommand("T1");
            
            var existingTemplate = new ExamTemplate 
            { 
                ExamTemplateId ="T1", 
                Name ="Base", 
                TemplateParts = new List<TemplatePart>
                {
                    new TemplatePart { Skill = QuestionSkill.Reading, Mark = 5 }
                } 
            };
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>())).ReturnsAsync(existingTemplate);
            
            _mockIdGen.Setup(x => x.GenerateCustom(10)).Returns("NEW_ID");
            _mockTemplateRepo.Setup(x => x.IsNameExistsAsync("Base (1)", null)).ReturnsAsync(false);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            
            _mockPartRepo.Verify(x => x.AddRangeAsync(It.Is<IEnumerable<TemplatePart>>(parts => parts.First().ExamTemplateId =="NEW_ID" && parts.First().TemplatePartId =="NEW_ID")), Times.Once);

            QACollector.LogTestCase("ExamTemplate - Duplicate", new TestCaseDetail
            {
                FunctionGroup     ="DuplicateExamTemplateCommandHandler",
                TestCaseID        ="DuplicateExamTemplateCommandHandler_03",
                Description       ="Parts",
                ExpectedResult    ="Adds",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Existing" }
            });
        }

        [Fact]
        public async Task Handle_DuplicateNameGeneration_GeneratesProperSuffix()
        {
             SetupHttpContext("U1");
            var command = new DuplicateExamTemplateCommand("T1");
            
            var existingTemplate = new ExamTemplate { Name ="Base (1)" };
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>())).ReturnsAsync(existingTemplate);
            
            // Should test stripping logic -> cleanName is"Base"
            _mockTemplateRepo.SetupSequence(x => x.IsNameExistsAsync(It.IsAny<string>(), null))
                             .ReturnsAsync(true)  //"Base (1)" exists
                             .ReturnsAsync(false); //"Base (2)" free

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            
            _mockTemplateRepo.Verify(x => x.AddAsync(It.Is<ExamTemplate>(t => t.Name =="Base (2)")), Times.Once);

            QACollector.LogTestCase("ExamTemplate - Duplicate", new TestCaseDetail
            {
                FunctionGroup     ="DuplicateExamTemplateCommandHandler",
                TestCaseID        ="DuplicateExamTemplateCommandHandler_04",
                Description       ="Regex",
                ExpectedResult    ="Name",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"While" }
            });
        }

        [Fact]
        public async Task Handle_UserIdExtractionSub_ExtractsFromClaims()
        {
            // Setup with SUB instead of NameIdentifier
            var claims = new List<Claim> { new Claim("sub","U2") };
            var identity = new ClaimsIdentity(claims,"TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            _mockHttpContext.Setup(x => x.HttpContext).Returns(httpContext);

            var command = new DuplicateExamTemplateCommand("T1");
            
            var existingTemplate = new ExamTemplate { Name ="Base" };
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>())).ReturnsAsync(existingTemplate);
            
            _mockTemplateRepo.Setup(x => x.IsNameExistsAsync("Base (1)", null)).ReturnsAsync(false);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            
            _mockTemplateRepo.Verify(x => x.AddAsync(It.Is<ExamTemplate>(t => t.CreatedBy =="U2")), Times.Once);

            QACollector.LogTestCase("ExamTemplate - Duplicate", new TestCaseDetail
            {
                FunctionGroup     ="DuplicateExamTemplateCommandHandler",
                TestCaseID        ="DuplicateExamTemplateCommandHandler_05",
                Description       ="Checking",
                ExpectedResult    ="Test",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Claims" }
            });
        }

        [Fact]
        public async Task Handle_Exception_Returns500ServerError()
        {
            var command = new DuplicateExamTemplateCommand("T1");
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>()))
                             .ThrowsAsync(new Exception("Database error"));

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("ExamTemplate - Duplicate", new TestCaseDetail
            {
                FunctionGroup     ="DuplicateExamTemplateCommandHandler",
                TestCaseID        ="DuplicateExamTemplateCommandHandler_06",
                Description       ="Error",
                ExpectedResult    ="Test",
                StatusRound1      ="Passed",
                TestCaseType      ="E",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Catch" }
            });
        }
    }
}
