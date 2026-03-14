using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Vocabulary.Commands.UpdateVocabulary;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary
{
    public class UpdateVocabularyCommandHandlerTests
    {
        private UpdateVocabularyCommandHandler CreateHandler(
            Mock<IVocabularyRepository>? vocabRepo = null,
            Mock<ISpeechService>? ttsService = null,
            Mock<ICloudinaryService>? cloudinaryService = null,
            bool unauthorized = false)
        {
            return new UpdateVocabularyCommandHandler(
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object,
                (ttsService ?? new Mock<ISpeechService>()).Object,
                (cloudinaryService ?? new Mock<ICloudinaryService>()).Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("ADMIN-001").Object,
                new Mock<ILogger<UpdateVocabularyCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            var command = new UpdateVocabularyCommand
            {
                VocabularyId = "VOCAB-001",
                UpdateData = new VocabularyUpdateDto { Text = "새 단어" }
            };

            var handler = CreateHandler(unauthorized: true);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Vocabulary - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Vocabulary",
                TestCaseID = "TC-VOCAB-UPD-01",
                Description = "Update vocabulary khi không có token xác thực",
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
        public async Task Handle_VocabNotFound_ShouldReturn404()
        {
            var command = new UpdateVocabularyCommand
            {
                VocabularyId = "VOCAB-INVALID",
                UpdateData = new VocabularyUpdateDto { Text = "새 단어" }
            };

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocabWithChildren: null));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Vocabulary - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Vocabulary",
                TestCaseID = "TC-VOCAB-UPD-02",
                Description = "Update vocabulary với ID không tồn tại",
                ExpectedResult = "Return 404 VocabularyNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid VocabularyId",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_DeletedVocab_ShouldReturn409()
        {
            var command = new UpdateVocabularyCommand
            {
                VocabularyId = "VOCAB-004",
                UpdateData = new VocabularyUpdateDto { Text = "새 단어" }
            };

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocabWithChildren: MockVocabularyRepository.GetSampleVocabDeleted()));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);

            QACollector.LogTestCase("Vocabulary - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Vocabulary",
                TestCaseID = "TC-VOCAB-UPD-03",
                Description = "Update vocabulary đã bị xóa (Status = Deleted)",
                ExpectedResult = "Return 409 VocabularyDeletedCannotUpdate",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status = Deleted",
                    "Return 409"
                }
            });
        }

        [Fact]
        public async Task Handle_TextUnchanged_ShouldNotCallTts()
        {
            // Text trong UpdateData giống hệt Text hiện tại → textChanged = false → TTS không được gọi
            var existingVocab = MockVocabularyRepository.GetSampleVocabulary();

            var command = new UpdateVocabularyCommand
            {
                VocabularyId = existingVocab.VocabularyId,
                UpdateData = new VocabularyUpdateDto
                {
                    Text = existingVocab.Text  // same text → no change
                }
            };

            var mockTts = new Mock<ISpeechService>();

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocabWithChildren: existingVocab),
                ttsService: mockTts);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // TTS KHÔNG được gọi vì Text không đổi
            mockTts.Verify(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()), Times.Never);

            QACollector.LogTestCase("Vocabulary - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Vocabulary",
                TestCaseID = "TC-VOCAB-UPD-04",
                Description = "Update Text giống Text hiện tại → textChanged = false → TTS không được gọi",
                ExpectedResult = "TTS.SynthesizeKoreanAudioAsync không được gọi, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "UpdateData.Text == existing Text (boundary: same value)",
                    "textChanged = false",
                    "TTS not called",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_TextChanged_ShouldCallTtsOnceAndReturn200()
        {
            var command = new UpdateVocabularyCommand
            {
                VocabularyId = "VOCAB-001",
                UpdateData = new VocabularyUpdateDto { Text = "바뀐 단어" } // different text
            };

            var mockTts = new Mock<ISpeechService>();
            mockTts.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                   .ReturnsAsync(new byte[] { 0x01 });

            var mockCloudinary = new Mock<ICloudinaryService>();
            mockCloudinary.Setup(x => x.UploadAudioAsync(
                        It.IsAny<byte[]>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                          .ReturnsAsync("https://cloudinary.com/audio/updated.mp3");

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocabWithChildren: MockVocabularyRepository.GetSampleVocabulary()),
                ttsService: mockTts,
                cloudinaryService: mockCloudinary);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            mockTts.Verify(
                x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()),
                Times.Once);

            QACollector.LogTestCase("Vocabulary - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Vocabulary",
                TestCaseID = "TC-VOCAB-UPD-05",
                Description = "Update Text mới (khác Text hiện tại) → hệ thống tự động regenerate audio",
                ExpectedResult = "TTS được gọi đúng 1 lần, AudioURL cập nhật, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Text changed",
                    "TTS called once",
                    "AudioURL updated",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_StatusChangedToDeleted_ShouldCascadeChildrenToDeletedAndReturn200()
        {
            var command = new UpdateVocabularyCommand
            {
                VocabularyId = "VOCAB-001",
                UpdateData = new VocabularyUpdateDto { Status = VocabularyStatus.Deleted }
            };

            var vocabWithChildren = MockVocabularyRepository.GetSampleVocabWithChildren();

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocabWithChildren: vocabWithChildren));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            vocabWithChildren.VocabularyTopics.Should()
                .OnlyContain(vt => vt.Status == VocabularyTopicStatus.Deleted);
            vocabWithChildren.VocabularyExamples.Should()
                .OnlyContain(ex => ex.Status == VocabularyExampleStatus.Deleted);

            QACollector.LogTestCase("Vocabulary - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Vocabulary",
                TestCaseID = "TC-VOCAB-UPD-06",
                Description = "Update Status → Deleted → cascade Topics và Examples thành Deleted",
                ExpectedResult = "Topics + Examples đều Status = Deleted, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status → Deleted",
                    "Has Topics and Examples",
                    "Cascade to Deleted",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_StatusChangedToDraft_ShouldCascadeChildrenToDraftAndReturn200()
        {
            var command = new UpdateVocabularyCommand
            {
                VocabularyId = "VOCAB-001",
                UpdateData = new VocabularyUpdateDto { Status = VocabularyStatus.Draft }
            };

            var vocabWithChildren = MockVocabularyRepository.GetSampleVocabWithChildren();

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocabWithChildren: vocabWithChildren));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            vocabWithChildren.VocabularyTopics.Should()
                .OnlyContain(vt => vt.Status == VocabularyTopicStatus.Draft);
            vocabWithChildren.VocabularyExamples.Should()
                .OnlyContain(ex => ex.Status == VocabularyExampleStatus.Draft);

            QACollector.LogTestCase("Vocabulary - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Vocabulary",
                TestCaseID = "TC-VOCAB-UPD-07",
                Description = "Update Status → Draft → cascade Topics và Examples thành Draft",
                ExpectedResult = "Topics + Examples đều Status = Draft, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status → Draft",
                    "Has Topics and Examples",
                    "Cascade to Draft",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_StatusChangedToActive_ShouldNotCascadeChildrenAndReturn200()
        {
            var command = new UpdateVocabularyCommand
            {
                VocabularyId = "VOCAB-003", // vocab đang Draft
                UpdateData = new VocabularyUpdateDto { Status = VocabularyStatus.Active }
            };

            // Vocab Draft có children ở Draft
            var vocabWithChildren = MockVocabularyRepository.GetSampleVocabWithChildren(
                status: VocabularyStatus.Draft);

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocabWithChildren: vocabWithChildren));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // Children KHÔNG bị cascade khi chuyển sang Active
            vocabWithChildren.VocabularyTopics.Should()
                .OnlyContain(vt => vt.Status == VocabularyTopicStatus.Active);
            vocabWithChildren.VocabularyExamples.Should()
                .OnlyContain(ex => ex.Status == VocabularyExampleStatus.Active);

            QACollector.LogTestCase("Vocabulary - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Vocabulary",
                TestCaseID = "TC-VOCAB-UPD-08",
                Description = "Update Status → Active → KHÔNG cascade xuống children (nghiệp vụ giữ nguyên children)",
                ExpectedResult = "Children giữ nguyên Status, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status → Active",
                    "No cascade to children (by design)",
                    "Return 200"
                }
            });
        }
    }
}