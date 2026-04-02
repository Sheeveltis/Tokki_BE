using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Vocabulary.Commands.CreateVocabularyByStaff;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary
{
    public class CreateVocabularyByStaffCommandHandlerTests
    {
        private CreateVocabularyByStaffCommandHandler CreateHandler(
            Mock<IVocabularyRepository>? vocabRepo = null,
            Mock<ISpeechService>? ttsService = null,
            Mock<ICloudinaryService>? cloudinaryService = null,
            bool unauthorized = false)
        {
            return new CreateVocabularyByStaffCommandHandler(
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object,
                MockVocabularyExampleRepository.GetMock().Object,
                MockIdGeneratorService.GetMock().Object,
                (ttsService ?? new Mock<ISpeechService>()).Object,
                (cloudinaryService ?? new Mock<ICloudinaryService>()).Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("STAFF-001").Object,
                new Mock<ILogger<CreateVocabularyByStaffCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_ValidData_ShouldReturnDraftStatus201()
        {
            var command = new CreateVocabularyByStaffCommand
            {
                Text = "직원 단어",
                Definition = "Từ của nhân viên"
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock(
                returnedByText: new List<Tokki.Domain.Entities.Vocabulary>());

            var mockTts = new Mock<ISpeechService>();
            mockTts.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                   .ReturnsAsync(new byte[] { 0x01 });

            var mockCloudinary = new Mock<ICloudinaryService>();
            mockCloudinary.Setup(x => x.UploadAudioAsync(
                        It.IsAny<byte[]>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                          .ReturnsAsync("https://cloudinary.com/audio/staff.mp3");

            var handler = CreateHandler(
                vocabRepo: mockVocabRepo,
                ttsService: mockTts,
                cloudinaryService: mockCloudinary);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Message.Should().Contain("chờ phê duyệt");

            QACollector.LogTestCase("Vocabulary - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "Create Vocabulary By Staff",
                TestCaseID = "TC-VOCAB-STA-01",
                Description = "Staff tạo vocabulary hợp lệ → Status = Draft, message chờ phê duyệt",
                ExpectedResult = "Return 201, message chứa 'chờ phê duyệt'",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Staff role",
                    "No duplicate",
                    "Status = Draft",
                    "Return 201"
                }
            });
        }

        [Fact]
        public async Task Handle_DuplicateVocab_ShouldReturn400()
        {
            var command = new CreateVocabularyByStaffCommand
            {
                Text = "안녕하세요",
                Definition = "Xin chào"
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock(
                returnedByText: new List<Tokki.Domain.Entities.Vocabulary>
                {
                    MockVocabularyRepository.GetSampleVocabulary()
                });

            var handler = CreateHandler(vocabRepo: mockVocabRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Vocabulary - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "Create Vocabulary By Staff",
                TestCaseID = "TC-VOCAB-STA-02",
                Description = "Staff tạo vocabulary trùng Text + Definition đã tồn tại",
                ExpectedResult = "Return 400 Vocabulary.Duplicated",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Text + Definition duplicate",
                    "Return 400"
                }
            });
        }
    }
}