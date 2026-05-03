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
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Application.UseCases.VocabularyExample.Commands.AddExamples;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.VocabularyExample.Commands.AddExamples
{
    public class AddVocabularyExamplesCommandHandlerTests
    {
        private readonly Mock<IVocabularyRepository> _vocabRepoMock = new();
        private readonly Mock<IVocabularyExampleRepository> _exampleRepoMock = new();
        private readonly Mock<IIdGeneratorService> _idGenMock = new();
        private readonly Mock<IHttpContextAccessor> _httpMock = new();

        public AddVocabularyExamplesCommandHandlerTests()
        {
            SetupHttpContext("USER-001");
            _idGenMock.Setup(x => x.Generate(It.IsAny<int>())).Returns("ID-GEN");
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

        private AddVocabularyExamplesCommandHandler CreateHandler() => new(
            _vocabRepoMock.Object,
            _exampleRepoMock.Object,
            _idGenMock.Object,
            _httpMock.Object,
            NullLogger<AddVocabularyExamplesCommandHandler>.Instance
        );

        // ═══════════════════════════════════════════════════════════
        // AddVocabularyExamplesCommandHandler_01 | A | No User -> 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            SetupHttpContext(null);
            var command = new AddVocabularyExamplesCommand { VocabularyId = "V1" };
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Vocabulary Example - Add", new TestCaseDetail
            {
                FunctionGroup = "AddVocabularyExamplesCommandHandler",
                TestCaseID = "AddVocabularyExamplesCommandHandler_01",
                Description = "Returns error if not logged in",
                ExpectedResult = "Return 401",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "HttpContext User is null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // AddVocabularyExamplesCommandHandler_02 | A | Vocab ID Empty -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyVocabId_ShouldReturn400()
        {
            var command = new AddVocabularyExamplesCommand { VocabularyId = "" };
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Vocabulary Example - Add", new TestCaseDetail
            {
                FunctionGroup = "AddVocabularyExamplesCommandHandler",
                TestCaseID = "AddVocabularyExamplesCommandHandler_02",
                Description = "Vocab ID missing validation check",
                ExpectedResult = "Return 400",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VocabularyId is empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // AddVocabularyExamplesCommandHandler_03 | A | Examples Empty -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyExamples_ShouldReturn400()
        {
            var command = new AddVocabularyExamplesCommand 
            { 
                VocabularyId = "V1", 
                Examples = new List<VocabularyExampleDto>() 
            };
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Vocabulary Example - Add", new TestCaseDetail
            {
                FunctionGroup = "AddVocabularyExamplesCommandHandler",
                TestCaseID = "AddVocabularyExamplesCommandHandler_03",
                Description = "Empty request examples collection validation",
                ExpectedResult = "Return 400",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Examples array is empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // AddVocabularyExamplesCommandHandler_04 | A | Vocab Not Found -> 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabNotFound_ShouldReturn404()
        {
            var command = new AddVocabularyExamplesCommand 
            { 
                VocabularyId = "V-NONE", 
                Examples = new List<VocabularyExampleDto> { new() { Sentence = "A" } } 
            };
            _vocabRepoMock.Setup(x => x.GetByIdAsync("V-NONE")).ReturnsAsync((Domain.Entities.Vocabulary)null);

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Vocabulary Example - Add", new TestCaseDetail
            {
                FunctionGroup = "AddVocabularyExamplesCommandHandler",
                TestCaseID = "AddVocabularyExamplesCommandHandler_04",
                Description = "Checks if target vocabulary exists",
                ExpectedResult = "Return 404",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Vocabulary not found" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // AddVocabularyExamplesCommandHandler_05 | A | Blank Sentence inside Collection -> fails entirely 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_BlankSentence_ShouldReturn400()
        {
            var command = new AddVocabularyExamplesCommand 
            { 
                VocabularyId = "V1", 
                Examples = new List<VocabularyExampleDto> { new() { Sentence = "  " } } 
            };
            _vocabRepoMock.Setup(x => x.GetByIdAsync("V1")).ReturnsAsync(new Domain.Entities.Vocabulary());

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Vocabulary Example - Add", new TestCaseDetail
            {
                FunctionGroup = "AddVocabularyExamplesCommandHandler",
                TestCaseID = "AddVocabularyExamplesCommandHandler_05",
                Description = "Throws error if any of target sentences are empty",
                ExpectedResult = "Return 400",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "One item has blank sentence" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // AddVocabularyExamplesCommandHandler_06 | N | Includes duplicate and valid items -> skips dup cleanly
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidAndDuplicate_ShouldAddValidAndSkipDup()
        {
            var command = new AddVocabularyExamplesCommand 
            { 
                VocabularyId = "V1", 
                Examples = new List<VocabularyExampleDto> 
                { 
                    new() { Sentence = "Dup in DB" },
                    new() { Sentence = "New Ex" }
                } 
            };
            _vocabRepoMock.Setup(x => x.GetByIdAsync("V1")).ReturnsAsync(new Domain.Entities.Vocabulary());
            _exampleRepoMock.Setup(x => x.GetBySentenceAsync("V1", "Dup in DB"))
                            .ReturnsAsync(new Domain.Entities.VocabularyExample());

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.CreatedExamples.Should().HaveCount(1);
            result.Data.SkippedSentences.Should().HaveCount(1);
            
            _exampleRepoMock.Verify(x => x.AddAsync(It.Is<Domain.Entities.VocabularyExample>(e => e.Sentence == "New Ex")), Times.Once);
            _exampleRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Vocabulary Example - Add", new TestCaseDetail
            {
                FunctionGroup = "AddVocabularyExamplesCommandHandler",
                TestCaseID = "AddVocabularyExamplesCommandHandler_06",
                Description = "Properly filters and inserts new examples only",
                ExpectedResult = "Return 201 with Created=1, Skipped=1",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Mixed duplicates and valid items" }
            });
        }
    }
}
