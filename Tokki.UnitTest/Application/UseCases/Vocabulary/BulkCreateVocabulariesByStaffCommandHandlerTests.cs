using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Vocabulary.Commands.BulkCreateVocabulariesByStaff;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary
{
    public class BulkCreateVocabulariesByStaffCommandHandlerTests
    {
        private BulkCreateVocabulariesByStaffCommandHandler CreateHandler(
            Mock<IVocabularyRepository>? vocabRepo = null,
            Mock<IVocabularyExampleRepository>? exampleRepo = null,
            Mock<ISpeechService>? ttsService = null,
            Mock<ICloudinaryService>? cloudinaryService = null,
            bool unauthorized = false)
        {
            return new BulkCreateVocabulariesByStaffCommandHandler(
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object,
                (exampleRepo ?? MockVocabularyExampleRepository.GetMock()).Object,
                MockIdGeneratorService.GetMock().Object,
                (ttsService ?? new Mock<ISpeechService>()).Object,
                (cloudinaryService ?? new Mock<ICloudinaryService>()).Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("STAFF-001").Object,
                new Mock<ILogger<BulkCreateVocabulariesByStaffCommandHandler>>().Object);
        }

        // ═══════════════════════════════════════════════════════════
        // Bulk_Create_Vocabulary_By_Staff_01 | A | No token → 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            // Arrange
            var command = new BulkCreateVocabulariesByStaffCommand
            {
                Vocabularies = new List<VocabularyCreateDto>
                {
                    new VocabularyCreateDto { Text = "직원1", Definition = "Staff 1" }
                }
            };
            var handler = CreateHandler(unauthorized: true);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Bulk Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "Bulk Create Vocabulary By Staff",
                TestCaseID        = "Bulk_Create_Vocabulary_By_Staff_01",
                Description       = "Staff bulk creates vocabulary without authentication token",
                ExpectedResult    = "Return 401 Unauthorized",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No UserId in Claims", "Return 401" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Bulk_Create_Vocabulary_By_Staff_02 | A | Duplicate vocab → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_HasDuplicateVocab_ShouldReturn400()
        {
            // Arrange
            var command = new BulkCreateVocabulariesByStaffCommand
            {
                Vocabularies = new List<VocabularyCreateDto>
                {
                    new VocabularyCreateDto { Text = "안녕", Definition = "Hello" }
                }
            };
            var mockVocabRepo = MockVocabularyRepository.GetMock(
                returnedByTextAndDefinition: MockVocabularyRepository.GetSampleVocabulary());
            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Bulk Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "Bulk Create Vocabulary By Staff",
                TestCaseID        = "Bulk_Create_Vocabulary_By_Staff_02",
                Description       = "Staff bulk create has identical vocab Text + Definition → reject all",
                ExpectedResult    = "Return 400 VOCABULARY_DUPLICATE",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Duplicate vocab (Text + Definition)", "All-or-nothing policy", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Bulk_Create_Vocabulary_By_Staff_03 | N | Valid list → Draft status → 201
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidList_ShouldReturnDraftStatusAndReturn201()
        {
            // Arrange
            var command = new BulkCreateVocabulariesByStaffCommand
            {
                Vocabularies = new List<VocabularyCreateDto>
                {
                    new VocabularyCreateDto { Text = "직원1", Definition = "Staff 1" },
                    new VocabularyCreateDto { Text = "직원2", Definition = "Staff 2" }
                }
            };
            var mockVocabRepo = MockVocabularyRepository.GetMock(returnedByTextAndDefinition: null);

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
            result.Data.SuccessCount.Should().Be(2);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Bulk Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "Bulk Create Vocabulary By Staff",
                TestCaseID        = "Bulk_Create_Vocabulary_By_Staff_03",
                Description       = "Staff bulk create 2 valid vocabs → Status = Draft, awaiting approval",
                ExpectedResult    = "Return 201, SuccessCount = 2",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Staff role", "2 valid vocabs", "Status = Draft", "Return 201" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Bulk_Create_Vocabulary_By_Staff_04 | B | Duplicate example in request → skip → 201
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExampleDuplicateInRequest_ShouldSkipAndStillReturn201()
        {
            // Arrange
            var command = new BulkCreateVocabulariesByStaffCommand
            {
                Vocabularies = new List<VocabularyCreateDto>
                {
                    new VocabularyCreateDto
                    {
                        Text       = "직원1",
                        Definition = "Staff 1",
                        Examples   = new List<VocabularyExampleDto>
                        {
                            new VocabularyExampleDto { Sentence = "같은 문장.", Translation = "Same sentence." },
                            new VocabularyExampleDto { Sentence = "같은 문장.", Translation = "Duplicate sentence." }
                        }
                    }
                }
            };
            var mockVocabRepo = MockVocabularyRepository.GetMock(returnedByTextAndDefinition: null);
            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Results[0].Message.Should().Contain("Skip");

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Bulk Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "Bulk Create Vocabulary By Staff",
                TestCaseID        = "Bulk_Create_Vocabulary_By_Staff_04",
                Description       = "Staff bulk create has duplicate example Sentence → skip duplicate, still create vocab",
                ExpectedResult    = "Return 201, message contains 'Skip'",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "2 examples with same Sentence", "1 skipped", "Return 201" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Bulk_Create_Vocabulary_By_Staff_05 | A | TTS fails → vocab created, AudioURL = null → 201
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TtsFails_ShouldStillReturn201WithNullAudio()
        {
            // Arrange
            var command = new BulkCreateVocabulariesByStaffCommand
            {
                Vocabularies = new List<VocabularyCreateDto>
                {
                    new VocabularyCreateDto { Text = "직원3", Definition = "Staff 3" }
                }
            };
            var mockVocabRepo = MockVocabularyRepository.GetMock(returnedByTextAndDefinition: null);

            var mockTts = new Mock<ISpeechService>();
            mockTts.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                   .ThrowsAsync(new Exception("TTS unavailable"));

            var handler = CreateHandler(vocabRepo: mockVocabRepo, ttsService: mockTts);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Results[0].AudioURL.Should().BeNull();

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Bulk Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "Bulk Create Vocabulary By Staff",
                TestCaseID        = "Bulk_Create_Vocabulary_By_Staff_05",
                Description       = "TTS error in staff bulk create → vocab created with AudioURL = null",
                ExpectedResult    = "Return 201, AudioURL = null",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TTS throws exception", "Vocab created without audio", "Return 201" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Bulk_Create_Vocabulary_By_Staff_06 | A | Repository throws → rollback → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrowsInsideTransaction_ShouldRollbackAndReturn500()
        {
            // Arrange
            var command = new BulkCreateVocabulariesByStaffCommand
            {
                Vocabularies = new List<VocabularyCreateDto>
                {
                    new VocabularyCreateDto { Text = "직원4", Definition = "Staff 4" }
                }
            };
            var mockVocabRepo = MockVocabularyRepository.GetMock(returnedByTextAndDefinition: null);
            mockVocabRepo.Setup(x => x.AddAsync(It.IsAny<Tokki.Domain.Entities.Vocabulary>()))
                         .ThrowsAsync(new Exception("DB insert failed"));

            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Bulk Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "Bulk Create Vocabulary By Staff",
                TestCaseID        = "Bulk_Create_Vocabulary_By_Staff_06",
                Description       = "Repository throws exception during transaction → rollback → 500",
                ExpectedResult    = "Transaction rollback, return 500 Server Error",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AddAsync throws DB exception", "Transaction rollback", "Return 500" }
            });
        }
    }
}