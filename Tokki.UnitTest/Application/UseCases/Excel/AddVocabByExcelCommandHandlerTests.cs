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
using Tokki.Application.UseCases.ExamTemplates.Commands.UpdateExamTemplate;
using Tokki.Application.UseCases.Excel.Commands.AddVocabByExcel;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Excel
{
    public class AddVocabByExcelCommandHandlerTests
    {
        private static IFormFile CreateFakeFile(string name = "vocab.xlsx")
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(x => x.FileName).Returns(name);
            return mock.Object;
        }

        private AddVocabByExcelCommandHandler CreateHandler(
            Mock<IExcelService>? excelService = null,
            Mock<IVocabularyRepository>? vocabRepo = null,
            Mock<IVocabularyTopicRepository>? vocabTopicRepo = null,
            Mock<ICloudinaryService>? cloudinary = null,
            Mock<ISpeechService>? tts = null)
        {
            return new AddVocabByExcelCommandHandler(
                (excelService ?? new Mock<IExcelService>()).Object,
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object,
                (cloudinary ?? MockCloudinaryService.GetMock()).Object,
                new Mock<ILogger<AddVocabByExcelCommandHandler>>().Object,
                (tts ?? MockSpeechService.GetMock()).Object,
                MockIdGeneratorService.GetMock().Object,
                (vocabTopicRepo ?? new Mock<IVocabularyTopicRepository>()).Object);
        }

        [Fact]
        public async Task Handle_EmptyExcelFile_ShouldReturnFailure()
        {
            // Arrange
            var command = new AddVocabByExcelCommand
            {
                File = CreateFakeFile(),
                StaffId = "STAFF-001",
                TopicId = "TOPIC-001"
            };

            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractVocabularyDataAsync(It.IsAny<IFormFile>()))
                     .ReturnsAsync(new List<VocabularyExcelDTO>());

            var handler = CreateHandler(excelService: mockExcel);
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Excel - Add Vocab By Excel", new TestCaseDetail
            {
                FunctionGroup = "Add Vocab By Excel",
                TestCaseID = "TC-AVBE-01",
                Description = "Import file Excel rỗng (không có data) → return Failure",
                ExpectedResult = "Return Failure ExcelNoValidDataFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "ExtractVocabularyDataAsync returns empty list",
                    "Return Failure"
                }
            });
        }

        [Fact]
        public async Task Handle_AllVocabsAlreadyExistInTopic_ShouldReturn200WithNoOperation()
        {
            // Arrange — vocab đã tồn tại trong DB VÀ đã có trong topic → tất cả vào FailureList
            var command = new AddVocabByExcelCommand
            {
                File = CreateFakeFile(),
                StaffId = "STAFF-001",
                TopicId = "TOPIC-001"
            };

            var extractedData = new List<VocabularyExcelDTO>
            {
                new VocabularyExcelDTO { Text = "안녕", Definition = "Xin chào" }
            };

            var existingVocab = MockVocabularyRepository.GetSampleVocabulary("VOCAB-001");
            existingVocab.Text = "안녕";
            existingVocab.Definition = "Xin chào";

            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractVocabularyDataAsync(It.IsAny<IFormFile>()))
                     .ReturnsAsync(extractedData);

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetExistingVocabEntitiesAsync(
                        It.IsAny<List<(string, string)>>()))
                         .ReturnsAsync(new List<Tokki.Domain.Entities.Vocabulary> { existingVocab });

            var mockVocabTopicRepo = new Mock<IVocabularyTopicRepository>();
            // Vocab đã có trong topic
            mockVocabTopicRepo.Setup(x => x.GetVocabIdsByTopicIdAsync("TOPIC-001"))
                              .ReturnsAsync(new List<string> { "VOCAB-001" });

            var handler = CreateHandler(
                excelService: mockExcel,
                vocabRepo: mockVocabRepo,
                vocabTopicRepo: mockVocabTopicRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.FailureList.Should().HaveCount(1);
            result.Data.FailureList[0].Reason.Should().Contain("đã tồn tại trong Topic");

            QACollector.LogTestCase("Excel - Add Vocab By Excel", new TestCaseDetail
            {
                FunctionGroup = "Add Vocab By Excel",
                TestCaseID = "TC-AVBE-02",
                Description = "Vocab đã tồn tại trong DB và trong Topic → vào FailureList",
                ExpectedResult = "Return 200, FailureList.Count = 1",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Vocab tồn tại trong DB",
                    "Vocab đã trong Topic",
                    "FailureList.Count = 1"
                }
            });
        }

        [Fact]
        public async Task Handle_ExistingVocabNotInTopic_ShouldLinkToTopicAndReturn200()
        {
            // Arrange — vocab đã trong DB nhưng CHƯA trong topic → link vào topic
            var command = new AddVocabByExcelCommand
            {
                File = CreateFakeFile(),
                StaffId = "STAFF-001",
                TopicId = "TOPIC-001"
            };

            var extractedData = new List<VocabularyExcelDTO>
            {
                new VocabularyExcelDTO { Text = "안녕", Definition = "Xin chào" }
            };

            var existingVocab = MockVocabularyRepository.GetSampleVocabulary("VOCAB-001");
            existingVocab.Text = "안녕";
            existingVocab.Definition = "Xin chào";

            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractVocabularyDataAsync(It.IsAny<IFormFile>()))
                     .ReturnsAsync(extractedData);

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetExistingVocabEntitiesAsync(
                        It.IsAny<List<(string, string)>>()))
                         .ReturnsAsync(new List<Tokki.Domain.Entities.Vocabulary> { existingVocab });

            var mockVocabTopicRepo = new Mock<IVocabularyTopicRepository>();
            // Vocab CHƯA có trong topic
            mockVocabTopicRepo.Setup(x => x.GetVocabIdsByTopicIdAsync("TOPIC-001"))
                              .ReturnsAsync(new List<string>());

            mockVocabTopicRepo.Setup(x => x.AddOrReactivateVocabulariesToTopicAsync(
          It.IsAny<string>(),
          It.IsAny<List<Tokki.Domain.Entities.Vocabulary>>(),
          It.IsAny<string>(),
          It.IsAny<CancellationToken>()))
      .ReturnsAsync((1, 0, new List<string>()));

            var handler = CreateHandler(
                excelService: mockExcel,
                vocabRepo: mockVocabRepo,
                vocabTopicRepo: mockVocabTopicRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.LinkedExistingVocabList.Should().HaveCount(1);

            QACollector.LogTestCase("Excel - Add Vocab By Excel", new TestCaseDetail
            {
                FunctionGroup = "Add Vocab By Excel",
                TestCaseID = "TC-AVBE-03",
                Description = "Vocab đã trong DB nhưng chưa trong Topic → link vào Topic",
                ExpectedResult = "Return 200, LinkedExistingVocabList.Count = 1",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Vocab tồn tại trong DB",
                    "Vocab CHƯA trong Topic",
                    "Link vào Topic",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_NewVocab_ShouldCreateAndReturn200()
        {
            // Arrange — vocab hoàn toàn mới → tạo mới và link vào topic
            var command = new AddVocabByExcelCommand
            {
                File = CreateFakeFile(),
                StaffId = "STAFF-001",
                TopicId = "TOPIC-001"
            };

            var extractedData = new List<VocabularyExcelDTO>
            {
                new VocabularyExcelDTO
                {
                    Text = "새로운",
                    Definition = "Mới",
                    Pronunciation = "sae-ro-un"
                }
            };

            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractVocabularyDataAsync(It.IsAny<IFormFile>()))
                     .ReturnsAsync(extractedData);

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            // Không có vocab nào match → tất cả là mới
            mockVocabRepo.Setup(x => x.GetExistingVocabEntitiesAsync(
                        It.IsAny<List<(string, string)>>()))
                         .ReturnsAsync(new List<Tokki.Domain.Entities.Vocabulary>());
            mockVocabRepo.Setup(x => x.AddRangeAsync(
               It.IsAny<List<Tokki.Domain.Entities.Vocabulary>>()))
                .Returns(Task.CompletedTask);
            var mockVocabTopicRepo = new Mock<IVocabularyTopicRepository>();
            mockVocabTopicRepo.Setup(x => x.GetVocabIdsByTopicIdAsync(It.IsAny<string>()))
                              .ReturnsAsync(new List<string>());
            mockVocabTopicRepo.Setup(x => x.AddVocabulariesToTopicWithTransactionAsync(
           It.IsAny<string>(),
           It.IsAny<List<Tokki.Domain.Entities.Vocabulary>>(),
           It.IsAny<string>(),
           It.IsAny<CancellationToken>()))
       .ReturnsAsync((true, 1, new List<string>()));


            var handler = CreateHandler(
                excelService: mockExcel,
                vocabRepo: mockVocabRepo,
                vocabTopicRepo: mockVocabTopicRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.AddedNewVocabList.Should().HaveCount(1);

            QACollector.LogTestCase("Excel - Add Vocab By Excel", new TestCaseDetail
            {
                FunctionGroup = "Add Vocab By Excel",
                TestCaseID = "TC-AVBE-04",
                Description = "Vocab hoàn toàn mới → tạo mới và link vào Topic",
                ExpectedResult = "Return 200, AddedNewVocabList.Count = 1",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Vocab không tồn tại trong DB",
                    "Tạo mới vocab",
                    "Link vào Topic",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_DuplicateInFile_ShouldAddToFailureList()
        {
            // Arrange — 2 dòng cùng Text + Definition trong file → dòng 2 vào FailureList
            var command = new AddVocabByExcelCommand
            {
                File = CreateFakeFile(),
                StaffId = "STAFF-001"
                // TopicId = null → không link topic
            };

            var extractedData = new List<VocabularyExcelDTO>
            {
                new VocabularyExcelDTO { Text = "안녕", Definition = "Xin chào" },
                new VocabularyExcelDTO { Text = "안녕", Definition = "Xin chào" } // duplicate trong file
            };

            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractVocabularyDataAsync(It.IsAny<IFormFile>()))
                     .ReturnsAsync(extractedData);

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetExistingVocabEntitiesAsync(
                        It.IsAny<List<(string, string)>>()))
                         .ReturnsAsync(new List<Tokki.Domain.Entities.Vocabulary>());
            mockVocabRepo.Setup(x => x.AddRangeAsync(
                    It.IsAny<List<Tokki.Domain.Entities.Vocabulary>>()))
                .Returns(Task.CompletedTask);

            var handler = CreateHandler(
                excelService: mockExcel,
                vocabRepo: mockVocabRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.FailureList.Should().HaveCount(1);
            result.Data.FailureList[0].Reason.Should().Contain("lặp lại");
            result.Data.AddedNewVocabList.Should().HaveCount(1); // chỉ tạo 1

            QACollector.LogTestCase("Excel - Add Vocab By Excel", new TestCaseDetail
            {
                FunctionGroup = "Add Vocab By Excel",
                TestCaseID = "TC-AVBE-05",
                Description = "2 dòng cùng Text+Definition trong file → dòng 2 vào FailureList",
                ExpectedResult = "FailureList.Count = 1, AddedNewVocabList.Count = 1",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "2 dòng trùng nhau trong file (boundary: duplicate)",
                    "Dòng 2 vào FailureList",
                    "Chỉ tạo 1 vocab"
                }
            });
        }
    }
}