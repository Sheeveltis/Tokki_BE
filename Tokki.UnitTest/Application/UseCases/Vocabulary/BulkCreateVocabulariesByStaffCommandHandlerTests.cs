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

        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            var command = new BulkCreateVocabulariesByStaffCommand
            {
                Vocabularies = new List<VocabularyCreateDto>
                {
                    new VocabularyCreateDto { Text = "직원1", Definition = "Nhân viên 1" }
                }
            };

            var handler = CreateHandler(unauthorized: true);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Vocabulary - Bulk Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "Bulk Create Vocabulary By Staff",
                TestCaseID = "TC-VOCAB-BST-01",
                Description = "Staff bulk create vocabulary khi không có token xác thực",
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
            var command = new BulkCreateVocabulariesByStaffCommand
            {
                Vocabularies = new List<VocabularyCreateDto>
                {
                    new VocabularyCreateDto { Text = "안녕", Definition = "Xin chào" }
                }
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock(
                returnedByTextAndDefinition: MockVocabularyRepository.GetSampleVocabulary());

            var handler = CreateHandler(vocabRepo: mockVocabRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Vocabulary - Bulk Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "Bulk Create Vocabulary By Staff",
                TestCaseID = "TC-VOCAB-BST-02",
                Description = "Staff bulk create có vocab trùng Text + Definition → reject toàn bộ",
                ExpectedResult = "Return 400 VOCABULARY_DUPLICATE",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Duplicate vocab (Text + Definition)",
                    "All-or-nothing policy",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidList_ShouldReturnDraftStatusAndReturn201()
        {
            var command = new BulkCreateVocabulariesByStaffCommand
            {
                Vocabularies = new List<VocabularyCreateDto>
                {
                    new VocabularyCreateDto { Text = "직원1", Definition = "Nhân viên 1" },
                    new VocabularyCreateDto { Text = "직원2", Definition = "Nhân viên 2" }
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
                          .ReturnsAsync("https://cloudinary.com/audio/staff.mp3");

            var handler = CreateHandler(
                vocabRepo: mockVocabRepo,
                ttsService: mockTts,
                cloudinaryService: mockCloudinary);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.SuccessCount.Should().Be(2);
            result.Message.Should().Contain("chờ phê duyệt");

            QACollector.LogTestCase("Vocabulary - Bulk Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "Bulk Create Vocabulary By Staff",
                TestCaseID = "TC-VOCAB-BST-03",
                Description = "Staff bulk create 2 vocab hợp lệ → Status = Draft, chờ phê duyệt",
                ExpectedResult = "Return 201, SuccessCount = 2, message 'chờ phê duyệt'",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Staff role",
                    "2 valid vocabs, no duplicate",
                    "Status = Draft (not Active)",
                    "Return 201"
                }
            });
        }

        [Fact]
        public async Task Handle_ExampleDuplicateInRequest_ShouldSkipAndStillReturn201()
        {
            var command = new BulkCreateVocabulariesByStaffCommand
            {
                Vocabularies = new List<VocabularyCreateDto>
                {
                    new VocabularyCreateDto
                    {
                        Text = "직원1",
                        Definition = "Nhân viên 1",
                        Examples = new List<VocabularyExampleDto>
                        {
                            new VocabularyExampleDto
                            {
                                Sentence = "같은 문장.",
                                Translation = "Câu giống nhau."
                            },
                            new VocabularyExampleDto
                            {
                                Sentence = "같은 문장.", // duplicate
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
            result.Data.Results[0].Message.Should().Contain("Bỏ qua");

            QACollector.LogTestCase("Vocabulary - Bulk Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "Bulk Create Vocabulary By Staff",
                TestCaseID = "TC-VOCAB-BST-04",
                Description = "Staff bulk create có example trùng Sentence (case-insensitive) → bỏ qua câu trùng, vẫn tạo vocab",
                ExpectedResult = "Return 201, message chứa 'Bỏ qua'",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "2 examples cùng Sentence (boundary)",
                    "Staff dùng OrdinalIgnoreCase → case-insensitive check",
                    "1 example bị skip",
                    "Return 201"
                }
            });
        }
    }
}