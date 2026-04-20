using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Vocabulary.Commands.CreateVocabulary;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary.Commands.CreateVocabulary
{
    public class CreateVocabularyCommandHandlerTests
    {
        private readonly Mock<IVocabularyRepository> _vocabRepoMock = new();
        private readonly Mock<IVocabularyExampleRepository> _exampleRepoMock = new();
        private readonly Mock<IIdGeneratorService> _idGenMock = new();
        private readonly Mock<ISpeechService> _ttsMock = new();
        private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
        private readonly Mock<IHttpContextAccessor> _httpMock = new();
        private readonly Mock<IDbContextTransaction> _transactionMock = new();

        public CreateVocabularyCommandHandlerTests()
        {
            SetupHttpContext("USER-001");
            _idGenMock.Setup(x => x.Generate(It.IsAny<int>())).Returns("ID-GEN");
            
            // Setup transaction
            _exampleRepoMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                            .ReturnsAsync(_transactionMock.Object);
        }

        private void SetupHttpContext(string? userId)
        {
            var ctx = new Mock<HttpContext>();
            if (userId != null)
            {
                var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }));
                ctx.Setup(x => x.User).Returns(user);
            }
            _httpMock.Setup(x => x.HttpContext).Returns(ctx.Object);
        }

        private CreateVocabularyCommandHandler CreateHandler() => new(
            _vocabRepoMock.Object,
            _exampleRepoMock.Object,
            _idGenMock.Object,
            _ttsMock.Object,
            _cloudinaryMock.Object,
            _httpMock.Object,
            NullLogger<CreateVocabularyCommandHandler>.Instance
        );

        // -----------------------------------------------------------
        // CreateVocabularyCommandHandler_01 | A | No User -> 401
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            SetupHttpContext(null);
            var command = new CreateVocabularyCommand { Text = "hello", Definition = "xin chao" };
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Vocabulary - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateVocabularyCommandHandler",
                TestCaseID = "CreateVocabularyCommandHandler_01",
                Description = "Returns error if not logged in",
                ExpectedResult = "Return 401",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "HttpContext User is null" }
            });
        }

        // -----------------------------------------------------------
        // CreateVocabularyCommandHandler_02 | A | Duplicate Vocabulary -> 400
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_DuplicateVocab_ShouldReturn400()
        {
            var command = new CreateVocabularyCommand { Text = " hello", Definition = " xin chao" };
            
            // Normalize matching
            _vocabRepoMock.Setup(x => x.GetAllByTextAsync("hello"))
                          .ReturnsAsync(new List<Domain.Entities.Vocabulary> 
                          { 
                              new Domain.Entities.Vocabulary { Text = "hello", Definition = "xin chao" } 
                          });

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

            QACollector.LogTestCase("Vocabulary - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateVocabularyCommandHandler",
                TestCaseID = "CreateVocabularyCommandHandler_02",
                Description = "Returns error if text and definition exact match exists",
                ExpectedResult = "Return 400",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Duplicate check returns true" }
            });
        }

        // -----------------------------------------------------------
        // CreateVocabularyCommandHandler_03 | N | TTS Failure -> Swallows exact, continues -> 201
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_TtsThrows_ShouldSwallowAndCreate()
        {
            var command = new CreateVocabularyCommand { Text = "hello", Definition = "xin chao" };
            _vocabRepoMock.Setup(x => x.GetAllByTextAsync("hello")).ReturnsAsync(new List<Domain.Entities.Vocabulary>());
            
            _ttsMock.Setup(x => x.SynthesizeKoreanAudioAsync("hello"))
                    .ThrowsAsync(new Exception("TTS Error"));

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.AudioURL.Should().BeNull();

            _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Vocabulary - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateVocabularyCommandHandler",
                TestCaseID = "CreateVocabularyCommandHandler_03",
                Description = "Continues normal path even if TTS fails",
                ExpectedResult = "Return 201, AudioUrl is null",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SynthesizeKoreanAudioAsync throws Exception" }
            });
        }

        // -----------------------------------------------------------
        // CreateVocabularyCommandHandler_04 | N | Example skipping (Empty, duplicate req, duplicate db)
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_ExampleFiltering_ShouldFilterCorrectly()
        {
            var command = new CreateVocabularyCommand 
            { 
                Text = "hello", 
                Definition = "xin chao",
                Examples = new List<VocabularyExampleDto>
                {
                    new() { Sentence = "  " }, // Skipped: blank
                    new() { Sentence = "Hello world" }, // Added
                    new() { Sentence = "Hello world" }, // Skipped: duplicate in input
                    new() { Sentence = "Exists in DB" } // Skipped: Duplicate in DB
                }
            };
            
            _vocabRepoMock.Setup(x => x.GetAllByTextAsync("hello")).ReturnsAsync(new List<Domain.Entities.Vocabulary>());
            
            _exampleRepoMock.Setup(x => x.GetBySentenceAsync(It.IsAny<string>(), "Exists in DB"))
                            .ReturnsAsync(new Domain.Entities.VocabularyExample()); // Mock DB existence

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Examples.Should().HaveCount(1); // Only"Hello world" survives
            result.Data.Examples[0].Sentence.Should().Be("Hello world");

            QACollector.LogTestCase("Vocabulary - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateVocabularyCommandHandler",
                TestCaseID = "CreateVocabularyCommandHandler_04",
                Description = "Filters empty, array duplicated, and DB duplicated examples",
                ExpectedResult = "Return 201 with exactly 1 mapped example",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Mixed valid/invalid examples" }
            });
        }

        // -----------------------------------------------------------
        // CreateVocabularyCommandHandler_05 | N | Full Correct Path with Audio -> 201
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_SuccessWithAudio_ShouldReturn201()
        {
            var command = new CreateVocabularyCommand { Text = "hello", Definition = "xin chao" };
            _vocabRepoMock.Setup(x => x.GetAllByTextAsync("hello")).ReturnsAsync(new List<Domain.Entities.Vocabulary>());
            
            _ttsMock.Setup(x => x.SynthesizeKoreanAudioAsync("hello")).ReturnsAsync(new byte[] { 1 });
            _cloudinaryMock.Setup(x => x.UploadAudioAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
                           .ReturnsAsync("http://audio.mp3");

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.AudioURL.Should().Be("http://audio.mp3");

            _vocabRepoMock.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.Vocabulary>()), Times.Once);
            _vocabRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Vocabulary - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateVocabularyCommandHandler",
                TestCaseID = "CreateVocabularyCommandHandler_05",
                Description = "Successful vocab creation with audio generated",
                ExpectedResult = "Return 201 with AudioURL set",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TTS succeeds and Cloudinary succeeds" }
            });
        }

        // -----------------------------------------------------------
        // CreateVocabularyCommandHandler_06 | E | General Exception -> Rollback -> 500
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_Exception_ShouldRollbackAndReturn500()
        {
            var command = new CreateVocabularyCommand { Text = "hello", Definition = "xin chao" };
            _vocabRepoMock.Setup(x => x.GetAllByTextAsync("hello")).ThrowsAsync(new Exception("DB Failure"));
            
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            _transactionMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Vocabulary - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateVocabularyCommandHandler",
                TestCaseID = "CreateVocabularyCommandHandler_06",
                Description = "Catch any exception, rollback and return 500",
                ExpectedResult = "Return 500",
                StatusRound1 = "Passed",
                TestCaseType = "E",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DB throws Exception" }
            });
        }
    }
}
