using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Vocabulary.Commands.CreateVocabularyByStaff;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary.Commands.CreateVocabularyByStaff
{
    public class CreateVocabularyByStaffCommandHandlerTests
    {
        private readonly Mock<IVocabularyRepository> _vocabRepoMock = new();
        private readonly Mock<IVocabularyExampleRepository> _exampleRepoMock = new();
        private readonly Mock<IIdGeneratorService> _idGenMock = new();
        private readonly Mock<ISpeechService> _ttsMock = new();
        private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
        private readonly Mock<IHttpContextAccessor> _httpMock = new();
        private readonly Mock<IDbContextTransaction> _transactionMock = new();

        public CreateVocabularyByStaffCommandHandlerTests()
        {
            SetupHttpContext("STAFF-01");
            _idGenMock.Setup(x => x.Generate(It.IsAny<int>())).Returns("ID-GEN");
            
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

        private CreateVocabularyByStaffCommandHandler CreateHandler() => new(
            _vocabRepoMock.Object,
            _exampleRepoMock.Object,
            _idGenMock.Object,
            _ttsMock.Object,
            _cloudinaryMock.Object,
            _httpMock.Object,
            NullLogger<CreateVocabularyByStaffCommandHandler>.Instance
        );

        // ═══════════════════════════════════════════════════════════
        // TC-VOC-CRS-01 | A | No User -> 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            SetupHttpContext(null);
            var command = new CreateVocabularyByStaffCommand { Text = "hello", Definition = "xin chao" };
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Vocabulary - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateVocabularyByStaffCommandHandler",
                TestCaseID = "TC-VOC-CRS-01",
                Description = "Returns error if not logged in",
                ExpectedResult = "Return 401",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "HttpContext User is null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOC-CRS-02 | A | Duplicate Vocabulary -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DuplicateVocab_ShouldReturn400()
        {
            var command = new CreateVocabularyByStaffCommand { Text = " hello ", Definition = " xin chao " };
            
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

            QACollector.LogTestCase("Vocabulary - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateVocabularyByStaffCommandHandler",
                TestCaseID = "TC-VOC-CRS-02",
                Description = "Returns error if exact match duplicate exists",
                ExpectedResult = "Return 400",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Duplicate validation triggers" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOC-CRS-03 | N | Draft Status Enforced
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CreatesDraft_ShouldBeDraft()
        {
            var command = new CreateVocabularyByStaffCommand { Text = "hello", Definition = "xin chao" };
            _vocabRepoMock.Setup(x => x.GetAllByTextAsync("hello")).ReturnsAsync(new List<Domain.Entities.Vocabulary>());
            
            Domain.Entities.Vocabulary created = null;
            _vocabRepoMock.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.Vocabulary>()))
                          .Callback<Domain.Entities.Vocabulary>(v => created = v);

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            created.Should().NotBeNull();
            created.Status.Should().Be(VocabularyStatus.Draft);

            QACollector.LogTestCase("Vocabulary - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateVocabularyByStaffCommandHandler",
                TestCaseID = "TC-VOC-CRS-03",
                Description = "Staff created vocabulary must always be in Draft status",
                ExpectedResult = "Vocabulary object has Draft status",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Check DB status field" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOC-CRS-04 | N | Example skipping (Empty, duplicate req, DB duplicate)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExampleFiltering_ShouldFilterCorrectly()
        {
            var command = new CreateVocabularyByStaffCommand 
            { 
                Text = "hello", 
                Definition = "xin chao",
                Examples = new List<VocabularyExampleDto>
                {
                    new() { Sentence = "  " },
                    new() { Sentence = "Ex1" },
                    new() { Sentence = "Ex1" }, // Duplicate in req
                    new() { Sentence = "Ex2" } // Duplicate in DB setup
                }
            };
            
            _vocabRepoMock.Setup(x => x.GetAllByTextAsync("hello")).ReturnsAsync(new List<Domain.Entities.Vocabulary>());
            _exampleRepoMock.Setup(x => x.GetBySentenceAsync(It.IsAny<string>(), "Ex2"))
                            .ReturnsAsync(new Domain.Entities.VocabularyExample());

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Examples.Should().HaveCount(1);
            result.Message.Should().Contain("bỏ qua");

            QACollector.LogTestCase("Vocabulary - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateVocabularyByStaffCommandHandler",
                TestCaseID = "TC-VOC-CRS-04",
                Description = "Filters empty and duplicate examples gracefully",
                ExpectedResult = "Return 201 with exactly 1 mapped example",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Mixed invalid examples list" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOC-CRS-05 | N | Full Correct Path with Audio -> 201
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SuccessWithAudio_ShouldReturn201()
        {
            var command = new CreateVocabularyByStaffCommand { Text = "hello", Definition = "xin chao" };
            _vocabRepoMock.Setup(x => x.GetAllByTextAsync("hello")).ReturnsAsync(new List<Domain.Entities.Vocabulary>());
            
            _ttsMock.Setup(x => x.SynthesizeKoreanAudioAsync("hello")).ReturnsAsync(new byte[] { 1 });
            _cloudinaryMock.Setup(x => x.UploadAudioAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
                           .ReturnsAsync("http://audio.mp3");

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.AudioURL.Should().Be("http://audio.mp3");
            _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Vocabulary - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateVocabularyByStaffCommandHandler",
                TestCaseID = "TC-VOC-CRS-05",
                Description = "Successful vocab creation with audio generated",
                ExpectedResult = "Return 201 with AudioURL set",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TTS and Cloudinary active" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOC-CRS-06 | E | General Exception -> Rollback -> 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Exception_ShouldRollbackAndReturn500()
        {
            var command = new CreateVocabularyByStaffCommand { Text = "hello", Definition = "xin chao" };
            _vocabRepoMock.Setup(x => x.GetAllByTextAsync("hello")).ThrowsAsync(new Exception("DB Down"));
            
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            _transactionMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Vocabulary - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateVocabularyByStaffCommandHandler",
                TestCaseID = "TC-VOC-CRS-06",
                Description = "Rollbacks if database throws error",
                ExpectedResult = "Return 500 ServerError",
                StatusRound1 = "Passed",
                TestCaseType = "E",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Exception triggered inside try" }
            });
        }
    }
}
