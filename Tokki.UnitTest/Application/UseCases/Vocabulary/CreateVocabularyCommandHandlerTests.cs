using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Vocabulary.Commands.CreateVocabulary;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary
{
    public class CreateVocabularyCommandHandlerTests
    {
        private CreateVocabularyCommandHandler CreateHandler(
            Mock<IVocabularyRepository>? vocabRepo = null,
            Mock<IVocabularyExampleRepository>? exampleRepo = null,
            Mock<ISpeechService>? ttsService = null,
            Mock<ICloudinaryService>? cloudinaryService = null,
            bool unauthorized = false)
        {
            return new CreateVocabularyCommandHandler(
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object,
                (exampleRepo ?? MockVocabularyExampleRepository.GetMock()).Object,
                MockIdGeneratorService.GetMock().Object,
                (ttsService ?? new Mock<ISpeechService>()).Object,
                (cloudinaryService ?? new Mock<ICloudinaryService>()).Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("USER-001").Object,
                new Mock<ILogger<CreateVocabularyCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            var command = new CreateVocabularyCommand
            {
                Text = "안녕",
                Definition = "Xin chào"
            };

            var handler = CreateHandler(unauthorized: true);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Vocabulary - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Vocabulary",
                TestCaseID = "TC-VOCAB-CRE-01",
                Description = "Tạo vocabulary khi không có token xác thực",
                ExpectedResult = "Return 401 Unauthorized",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No UserId in Claims", "Return 401" }
            });
        }

        [Fact]
        public async Task Handle_DuplicateTextAndDefinition_ShouldReturn400()
        {
            var command = new CreateVocabularyCommand
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

            QACollector.LogTestCase("Vocabulary - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Vocabulary",
                TestCaseID = "TC-VOCAB-CRE-02",
                Description = "Tạo vocabulary với Text + Definition đã tồn tại",
                ExpectedResult = "Return 400 Vocabulary.Duplicated",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Text + Definition trùng vocab hiện có",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidData_ShouldReturn201WithAudioUrl()
        {
            var command = new CreateVocabularyCommand
            {
                Text = "새로운 단어",
                Definition = "Từ mới",
                Examples = new List<VocabularyExampleDto>
                {
                    new VocabularyExampleDto
                    {
                        Sentence = "새로운 단어예요.",
                        Translation = "Đây là từ mới."
                    }
                }
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
                          .ReturnsAsync("https://cloudinary.com/audio/test.mp3");

            var handler = CreateHandler(
                vocabRepo: mockVocabRepo,
                ttsService: mockTts,
                cloudinaryService: mockCloudinary);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.AudioURL.Should().NotBeNullOrEmpty();

            QACollector.LogTestCase("Vocabulary - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Vocabulary",
                TestCaseID = "TC-VOCAB-CRE-03",
                Description = "Tạo vocabulary hợp lệ với example và audio → tạo thành công",
                ExpectedResult = "Return 201, AudioURL được set",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "No duplicate",
                    "TTS success",
                    "Has examples",
                    "Return 201"
                }
            });
        }

        [Fact]
        public async Task Handle_TtsServiceFails_ShouldStillReturn201WithNullAudio()
        {
            var command = new CreateVocabularyCommand
            {
                Text = "새로운 단어",
                Definition = "Từ mới"
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock(
                returnedByText: new List<Tokki.Domain.Entities.Vocabulary>());

            var mockTts = new Mock<ISpeechService>();
            mockTts.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                   .ThrowsAsync(new Exception("TTS service unavailable"));

            var handler = CreateHandler(
                vocabRepo: mockVocabRepo,
                ttsService: mockTts);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.AudioURL.Should().BeNull();

            QACollector.LogTestCase("Vocabulary - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Vocabulary",
                TestCaseID = "TC-VOCAB-CRE-04",
                Description = "TTS service lỗi → vocab vẫn được tạo với AudioURL = null",
                ExpectedResult = "Return 201, AudioURL = null",
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
    }
}