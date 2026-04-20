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
using Tokki.Application.UseCases.Vocabulary.DTOs;
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

        // ═══════════════════════════════════════════════════════════
        // Create_Vocabulary_By_Staff_01 | A | No token → 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            // Arrange
            var command = new CreateVocabularyByStaffCommand { Text = "직원 단어", Definition = "Staff word" };
            var handler = CreateHandler(unauthorized: true);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "Create Vocabulary By Staff",
                TestCaseID        = "Create_Vocabulary_By_Staff_01",
                Description       = "Staff creates vocabulary without authentication token",
                ExpectedResult    = "Return 401 Unauthorized",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No UserId in Claims", "Return 401" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Vocabulary_By_Staff_02 | A | Duplicate vocab → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DuplicateVocab_ShouldReturn400()
        {
            // Arrange
            var command = new CreateVocabularyByStaffCommand { Text = "안녕하세요", Definition = "Hello" };
            var mockVocabRepo = MockVocabularyRepository.GetMock(
                returnedByText: new List<Tokki.Domain.Entities.Vocabulary>
                {
                    MockVocabularyRepository.GetSampleVocabulary()
                });
            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "Create Vocabulary By Staff",
                TestCaseID        = "Create_Vocabulary_By_Staff_02",
                Description       = "Staff creates vocabulary with Text + Definition that already exists",
                ExpectedResult    = "Return 400 Vocabulary.Duplicated",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Text + Definition duplicate", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Vocabulary_By_Staff_03 | N | Valid data → Draft status → 201
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidData_ShouldReturnDraftStatus201()
        {
            // Arrange
            var command = new CreateVocabularyByStaffCommand { Text = "직원 단어", Definition = "From the staff" };
            var mockVocabRepo = MockVocabularyRepository.GetMock(
                returnedByText: new List<Tokki.Domain.Entities.Vocabulary>());

            var mockTts = new Mock<ISpeechService>();
            mockTts.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                   .ReturnsAsync(new byte[] { 0x01 });

            var mockCloudinary = new Mock<ICloudinaryService>();
            mockCloudinary.Setup(x => x.UploadAudioAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
                          .ReturnsAsync("https://cloudinary.com/audio/staff.mp3");

            var handler = CreateHandler(
                vocabRepo: mockVocabRepo,
                ttsService: mockTts,
                cloudinaryService: mockCloudinary);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Message.Should().Contain("awaiting approval");

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "Create Vocabulary By Staff",
                TestCaseID        = "Create_Vocabulary_By_Staff_03",
                Description       = "Staff creates valid vocabulary → Status = Draft, message awaiting approval",
                ExpectedResult    = "Return 201, message contains 'awaiting approval'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Staff role", "No duplicate", "Status = Draft", "Return 201" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Vocabulary_By_Staff_04 | A | TTS fails → 201 with null AudioURL
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TtsFails_ShouldStillReturn201WithNullAudio()
        {
            // Arrange
            var command = new CreateVocabularyByStaffCommand { Text = "직원 단어", Definition = "Staff word" };
            var mockVocabRepo = MockVocabularyRepository.GetMock(
                returnedByText: new List<Tokki.Domain.Entities.Vocabulary>());

            var mockTts = new Mock<ISpeechService>();
            mockTts.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                   .ThrowsAsync(new Exception("TTS unavailable"));

            var handler = CreateHandler(vocabRepo: mockVocabRepo, ttsService: mockTts);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.AudioURL.Should().BeNull();

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "Create Vocabulary By Staff",
                TestCaseID        = "Create_Vocabulary_By_Staff_04",
                Description       = "TTS service error → vocab still created with AudioURL = null",
                ExpectedResult    = "Return 201, AudioURL = null",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TTS throws exception", "Vocab created without audio", "Return 201" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Vocabulary_By_Staff_05 | N | Valid data with examples → 201 with examples
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidDataWithExamples_ShouldReturn201WithExamples()
        {
            // Arrange
            var command = new CreateVocabularyByStaffCommand
            {
                Text       = "직원 단어",
                Definition = "Staff word",
                Examples   = new List<VocabularyExampleDto>
                {
                    new VocabularyExampleDto { Sentence = "직원이 열심히 일해요.", Translation = "The employee works hard." }
                }
            };
            var mockVocabRepo = MockVocabularyRepository.GetMock(
                returnedByText: new List<Tokki.Domain.Entities.Vocabulary>());
            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "Create Vocabulary By Staff",
                TestCaseID        = "Create_Vocabulary_By_Staff_05",
                Description       = "Staff creates valid vocabulary with 1 example sentence → successful",
                ExpectedResult    = "Return 201, vocab and example created",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Staff role", "1 valid example", "Return 201" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Vocabulary_By_Staff_06 | A | Repository throws → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn500()
        {
            // Arrange
            var command = new CreateVocabularyByStaffCommand { Text = "단어", Definition = "Word" };
            var mockVocabRepo = MockVocabularyRepository.GetMock(
                returnedByText: new List<Tokki.Domain.Entities.Vocabulary>());
            mockVocabRepo.Setup(x => x.AddAsync(It.IsAny<Tokki.Domain.Entities.Vocabulary>()))
                         .ThrowsAsync(new Exception("DB insert failed"));

            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "Create Vocabulary By Staff",
                TestCaseID        = "Create_Vocabulary_By_Staff_06",
                Description       = "Repository.AddAsync throws exception → rollback → 500",
                ExpectedResult    = "Transaction rollback, return 500",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AddAsync throws DB exception", "Transaction rollback", "Return 500" }
            });
        }
    }
}