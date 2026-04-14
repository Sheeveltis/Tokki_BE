using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.Commands.ImportPronunciationRules;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Excel.Commands
{
    public class ImportPronunciationRulesCommandHandlerTests
    {
        private readonly Mock<IExcelService> _mockExcel;
        private readonly Mock<IPronunciationRuleRepository> _mockRepo;
        private readonly Mock<IIdGeneratorService> _mockIdGen;
        private readonly Mock<ILogger<ImportPronunciationRulesCommandHandler>> _mockLogger;
        private readonly ImportPronunciationRulesCommandHandler _handler;

        public ImportPronunciationRulesCommandHandlerTests()
        {
            _mockExcel = new Mock<IExcelService>();
            _mockRepo = new Mock<IPronunciationRuleRepository>();
            _mockIdGen = new Mock<IIdGeneratorService>();
            _mockLogger = new Mock<ILogger<ImportPronunciationRulesCommandHandler>>();

            _handler = new ImportPronunciationRulesCommandHandler(
                _mockExcel.Object, _mockRepo.Object, _mockIdGen.Object, _mockLogger.Object);
        }

        // TC-EXC-IR-01 | A | Excel Empty -> Error
        [Fact]
        public async Task Handle_ExcelEmpty_ShouldReturnError()
        {
            _mockExcel.Setup(x => x.ExtractRuleDataAsync(It.IsAny<IFormFile>()))
                      .ReturnsAsync(new List<PronunciationRuleExcelDTO>());

            var command = new ImportPronunciationRulesCommand { File = new Mock<IFormFile>().Object };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("EXCEL_EMPTY");

            QACollector.LogTestCase("Excel - Import Rules", new TestCaseDetail
            {
                FunctionGroup = "ImportPronunciationRulesCommandHandler",
                TestCaseID = "TC-EXC-IR-01",
                Description = "Rejects immediately if parser returns blank collections",
                ExpectedResult = "EXCEL_EMPTY Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty List extraction" }
            });
        }

        // TC-EXC-IR-02 | A | Excel Null -> Error
        [Fact]
        public async Task Handle_ExcelNull_ShouldReturnError()
        {
            _mockExcel.Setup(x => x.ExtractRuleDataAsync(It.IsAny<IFormFile>()))
                      .ReturnsAsync((List<PronunciationRuleExcelDTO>)null);

            var command = new ImportPronunciationRulesCommand { File = new Mock<IFormFile>().Object };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("không tìm thấy dữ liệu hợp lệ");

            QACollector.LogTestCase("Excel - Import Rules", new TestCaseDetail
            {
                FunctionGroup = "ImportPronunciationRulesCommandHandler",
                TestCaseID = "TC-EXC-IR-02",
                Description = "Rejects completely null returns safely without NullReference breakpoints",
                ExpectedResult = "EXCEL_EMPTY Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Null List extraction" }
            });
        }

        // TC-EXC-IR-03 | A | DB Save Throws Exception
        [Fact]
        public async Task Handle_DatabaseException_ReturnsFailure()
        {
            var dtos = new List<PronunciationRuleExcelDTO> 
            { 
                new PronunciationRuleExcelDTO { RuleName = "R1" }
            };
            
            _mockExcel.Setup(x => x.ExtractRuleDataAsync(It.IsAny<IFormFile>()))
                      .ReturnsAsync(dtos);

            _mockIdGen.Setup(x => x.Generate(10)).Returns("ID1");
            _mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("DB Connection Lost"));

            var command = new ImportPronunciationRulesCommand { File = new Mock<IFormFile>().Object, UserId = "U1" };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("DATABASE_ERROR");

            QACollector.LogTestCase("Excel - Import Rules", new TestCaseDetail
            {
                FunctionGroup = "ImportPronunciationRulesCommandHandler",
                TestCaseID = "TC-EXC-IR-03",
                Description = "Wraps context execution faults protecting service loops",
                ExpectedResult = "DATABASE_ERROR",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DB Exception throw" }
            });
        }

        // TC-EXC-IR-04 | N | Single Row Error Processing (Catches silently)
        [Fact]
        public async Task Handle_SingleRowException_CapturesInFailureList()
        {
            var dtos = new List<PronunciationRuleExcelDTO> 
            { 
                new PronunciationRuleExcelDTO { RuleName = "BadRow" },
                new PronunciationRuleExcelDTO { RuleName = "GoodRow" }
            };

            _mockExcel.Setup(x => x.ExtractRuleDataAsync(It.IsAny<IFormFile>()))
                      .ReturnsAsync(dtos);

            // Force _idGen to throw on the 1st call, succeed on 2nd
            _mockIdGen.SetupSequence(x => x.Generate(10))
                      .Throws(new ArgumentException("Invalid logic param"))
                      .Returns("ID2");

            var command = new ImportPronunciationRulesCommand { File = new Mock<IFormFile>().Object, UserId = "U1" };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.FailureList.Should().HaveCount(1);
            result.Data.SuccessList.Should().HaveCount(1);
            result.Message.Should().Contain("Thành công: 1, Thất bại: 1");

            QACollector.LogTestCase("Excel - Import Rules", new TestCaseDetail
            {
                FunctionGroup = "ImportPronunciationRulesCommandHandler",
                TestCaseID = "TC-EXC-IR-04",
                Description = "Batch iterations continue independently on isolated row transformation failures",
                ExpectedResult = "Success Partial List",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "1 Throw, 1 Success mapping" }
            });
        }

        // TC-EXC-IR-05 | N | Full Batch Setup Succeds
        [Fact]
        public async Task Handle_SuccessfulBatch_ShouldSaveAndReturnTotal()
        {
            var dtos = new List<PronunciationRuleExcelDTO> 
            { 
                new PronunciationRuleExcelDTO { RuleName = "R1", SortOrder = 1 },
                new PronunciationRuleExcelDTO { RuleName = "R2" }
            };

            _mockExcel.Setup(x => x.ExtractRuleDataAsync(It.IsAny<IFormFile>()))
                      .ReturnsAsync(dtos);

            _mockIdGen.Setup(x => x.Generate(10)).Returns("IDX");

            var command = new ImportPronunciationRulesCommand { File = new Mock<IFormFile>().Object, UserId = "U1" };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.SuccessList.Should().HaveCount(2);
            result.Data.FailureList.Should().HaveCount(0);
            
            _mockRepo.Verify(x => x.AddRangeAsync(It.Is<List<Tokki.Domain.Entities.PronunciationRule>>(list => list.Count == 2)), Times.Once);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Excel - Import Rules", new TestCaseDetail
            {
                FunctionGroup = "ImportPronunciationRulesCommandHandler",
                TestCaseID = "TC-EXC-IR-05",
                Description = "Successful arrays bind all mapped attributes accurately committing perfectly",
                ExpectedResult = "Success Full List Commit",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Clean batch 2 inserts" }
            });
        }

        // TC-EXC-IR-06 | N | Verify Object Instantiation Flags Mapping
        [Fact]
        public async Task Handle_MapsTrackingAudits_ShouldVerifyFlags()
        {
            var dtos = new List<PronunciationRuleExcelDTO> 
            { 
                new PronunciationRuleExcelDTO { RuleName = "R1" }
            };

            _mockExcel.Setup(x => x.ExtractRuleDataAsync(It.IsAny<IFormFile>()))
                      .ReturnsAsync(dtos);

            Tokki.Domain.Entities.PronunciationRule mappedEntity = null;
            _mockRepo.Setup(x => x.AddRangeAsync(It.IsAny<List<Tokki.Domain.Entities.PronunciationRule>>()))
                     .Callback<IEnumerable<Tokki.Domain.Entities.PronunciationRule>>(rules => 
                     {
                         var list = (List<Tokki.Domain.Entities.PronunciationRule>)rules;
                         mappedEntity = list[0];
                     });

            var command = new ImportPronunciationRulesCommand { File = new Mock<IFormFile>().Object, UserId = "ADMIN" };
            await _handler.Handle(command, CancellationToken.None);

            mappedEntity.Should().NotBeNull();
            mappedEntity.CreateBy.Should().Be("ADMIN");
            mappedEntity.IsDeleted.Should().BeFalse();

            QACollector.LogTestCase("Excel - Import Rules", new TestCaseDetail
            {
                FunctionGroup = "ImportPronunciationRulesCommandHandler",
                TestCaseID = "TC-EXC-IR-06",
                Description = "Injected base flags initialize core tracking records precisely protecting state mechanics",
                ExpectedResult = "Audit fields verified true",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Tracks CreateBy and False Deletions" }
            });
        }
    }
}
