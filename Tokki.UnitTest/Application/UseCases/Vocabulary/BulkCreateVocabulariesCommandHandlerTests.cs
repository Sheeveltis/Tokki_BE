using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Vocabulary.Commands.BulkCreateVocabularies;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary
{
    public class BulkCreateVocabulariesCommandHandlerTests
    {
        private BulkCreateVocabulariesCommandHandler CreateHandler(
            Mock<IVocabularyRepository>? vocabRepo = null,
            Mock<IVocabularyExampleRepository>? exampleRepo = null,
            Mock<ISpeechService>? ttsService = null,
            Mock<ICloudinaryService>? cloudinaryService = null,
            bool unauthorized = false)
        {
            return new BulkCreateVocabulariesCommandHandler(
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object,
                (exampleRepo ?? MockVocabularyExampleRepository.GetMock()).Object,
                MockIdGeneratorService.GetMock().Object,
                (ttsService ?? new Mock<ISpeechService>()).Object,
                (cloudinaryService ?? new Mock<ICloudinaryService>()).Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("USER-001").Object,
                new Mock<ILogger<BulkCreateVocabulariesCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            var command = new BulkCreateVocabulariesCommand
            {
                Vocabularies = new List<VocabularyCreateDto>
                {
                    new VocabularyCreateDto { Text = "안녕", Definition = "Xin chào" }
                }
            };

            var handler = CreateHandler(unauthorized: true);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Vocabulary - Bulk Create", new TestCaseDetail
            {
                FunctionGroup = "Bulk Create Vocabulary",
                TestCaseID = "TC-VOCAB-BLK-01",
                Description = "Bulk create vocabulary khi không có token xác thực",
                ExpectedResult = "Return 401 Unauthorized",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "No UserId in Claims",
                    "Return 401"
                }
            });
        }

        [Fact]
        public async Task Handle_HasDuplicateVocab_ShouldReturn400()
        {
            var command = new BulkCreateVocabulariesCommand
            {
                Vocabularies = new List<VocabularyCreateDto>
                {
                    new VocabularyCreateDto { Text = "안녕", Definition = "Xin chào" }
                }
            };

            // GetByTextAndDefinitionAsync trả về vocab → duplicate
            var mockVocabRepo = MockVocabularyRepository.GetMock(
                returnedByTextAndDefinition: MockVocabularyRepository.GetSampleVocabulary());

            var handler = CreateHandler(vocabRepo: mockVocabRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Match(x =>
                x.Contains("VOCABULARY_DUPLICATE") || x.Contains("trùng"));
            QACollector.LogTestCase("Vocabulary - Bulk Create", new TestCaseDetail
            {
                FunctionGroup = "Bulk Create Vocabulary",
                TestCaseID = "TC-VOCAB-BLK-02",
                Description = "Bulk create có 1 vocab trùng Text + Definition → reject toàn bộ request",
                ExpectedResult = "Return 400 VOCABULARY_DUPLICATE",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "1 duplicate vocab (Text + Definition)",
                    "All-or-nothing policy",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidList_ShouldReturn201WithCorrectSuccessCount()
        {
            var command = new BulkCreateVocabulariesCommand
            {
                Vocabularies = new List<VocabularyCreateDto>
                {
                    new VocabularyCreateDto { Text = "단어1", Definition = "Từ 1" },
                    new VocabularyCreateDto { Text = "단어2", Definition = "Từ 2" }
                }
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock(
                returnedByTextAndDefinition: null);

            var mockTts = new Mock<ISpeechService>();
            mockTts.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                   .ReturnsAsync(new byte[] { 0x01 });

            var mockCloudinary = new Mock<ICloudinaryService>();
            mockCloudinary.Setup(x => x.UploadAudioAsync(
                        It.IsAny<byte[]>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                          .ReturnsAsync("https://cloudinary.com/audio/bulk.mp3");

            var handler = CreateHandler(
                vocabRepo: mockVocabRepo,
                ttsService: mockTts,
                cloudinaryService: mockCloudinary);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.SuccessCount.Should().Be(2);
            result.Data.TotalVocabularies.Should().Be(2);

            QACollector.LogTestCase("Vocabulary - Bulk Create", new TestCaseDetail
            {
                FunctionGroup = "Bulk Create Vocabulary",
                TestCaseID = "TC-VOCAB-BLK-03",
                Description = "Bulk create 2 vocab hợp lệ, không trùng → tất cả tạo thành công",
                ExpectedResult = "Return 201, SuccessCount = 2, TotalVocabularies = 2",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "2 valid vocabs",
                    "No duplicate",
                    "TTS success",
                    "Return 201, SuccessCount = 2"
                }
            });
        }

        [Fact]
        public async Task Handle_TtsServiceFails_ShouldStillCreateVocabWithNullAudio()
        {
            var command = new BulkCreateVocabulariesCommand
            {
                Vocabularies = new List<VocabularyCreateDto>
                {
                    new VocabularyCreateDto { Text = "단어1", Definition = "Từ 1" }
                }
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock(
                returnedByTextAndDefinition: null);

            // TTS throws → audioUrl = null nhưng vocab vẫn được tạo
            var mockTts = new Mock<ISpeechService>();
            mockTts.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                   .ThrowsAsync(new Exception("TTS unavailable"));

            var handler = CreateHandler(
                vocabRepo: mockVocabRepo,
                ttsService: mockTts);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.SuccessCount.Should().Be(1);
            result.Data.Results[0].AudioURL.Should().BeNull();

            QACollector.LogTestCase("Vocabulary - Bulk Create", new TestCaseDetail
            {
                FunctionGroup = "Bulk Create Vocabulary",
                TestCaseID = "TC-VOCAB-BLK-04",
                Description = "TTS lỗi trong bulk create → vocab vẫn được tạo với AudioURL = null",
                ExpectedResult = "Return 201, SuccessCount = 1, AudioURL = null",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "TTS throws exception",
                    "Vocab created without audio",
                    "Return 201"
                }
            });
        }

        [Fact]
        public async Task Handle_ExampleDuplicateInRequest_ShouldSkipDuplicateAndReturn201()
        {
            var command = new BulkCreateVocabulariesCommand
            {
                Vocabularies = new List<VocabularyCreateDto>
                {
                    new VocabularyCreateDto
                    {
                        Text = "단어1",
                        Definition = "Từ 1",
                        Examples = new List<VocabularyExampleDto>
                        {
                            new VocabularyExampleDto
                            {
                                Sentence = "같은 문장이에요.",
                                Translation = "Câu giống nhau."
                            },
                            new VocabularyExampleDto
                            {
                                Sentence = "같은 문장이에요.", // duplicate
                                Translation = "Câu bị trùng."
                            }
                        }
                    }
                }
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock(
                returnedByTextAndDefinition: null);

            var handler = CreateHandler(vocabRepo: mockVocabRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Results[0].Message.Should().Contain("bỏ qua");

            QACollector.LogTestCase("Vocabulary - Bulk Create", new TestCaseDetail
            {
                FunctionGroup = "Bulk Create Vocabulary",
                TestCaseID = "TC-VOCAB-BLK-05",
                Description = "Bulk create có example trùng Sentence trong cùng request → bỏ qua câu trùng, vẫn tạo vocab",
                ExpectedResult = "Return 201, message chứa 'bỏ qua', SuccessCount = 1",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "2 examples cùng Sentence (boundary: duplicate trong request)",
                    "1 example bị skip",
                    "Vocab vẫn được tạo",
                    "Return 201"
                }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrowsInsideTransaction_ShouldRollbackAndReturn500()
        {
            var command = new BulkCreateVocabulariesCommand
            {
                Vocabularies = new List<VocabularyCreateDto>
                {
                    new VocabularyCreateDto { Text = "단어1", Definition = "Từ 1" }
                }
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock(
                returnedByTextAndDefinition: null);

            // AddAsync throws → transaction rollback → 500
            mockVocabRepo.Setup(x => x.AddAsync(It.IsAny<Tokki.Domain.Entities.Vocabulary>()))
                         .ThrowsAsync(new Exception("DB insert failed"));

            var handler = CreateHandler(vocabRepo: mockVocabRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Vocabulary - Bulk Create", new TestCaseDetail
            {
                FunctionGroup = "Bulk Create Vocabulary",
                TestCaseID = "TC-VOCAB-BLK-06",
                Description = "Repository throw exception trong transaction → rollback và return 500",
                ExpectedResult = "Transaction rollback, return 500 Server Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "AddAsync throws DB exception",
                    "Transaction rollback",
                    "Return 500"
                }
            });
        }
    }
}