using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.ExamTemplates.Commands.DuplicateExamTemplate;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.ExamTemplates.Commands
{
    public class DuplicateExamTemplateHandlerTests : ExamTemplateTestBase
    {
        private readonly DuplicateExamTemplateCommandHandler _handler;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

        public DuplicateExamTemplateHandlerTests()
        {
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            var userId = "TEST_USER_ID";
            var context = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("sub", userId)
            };
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(context);

            var logger = new Mock<ILogger<DuplicateExamTemplateCommandHandler>>();

            _handler = new DuplicateExamTemplateCommandHandler(
                _mockExamTemplateRepo.Object,
                _mockTemplatePartRepo.Object,
                _mockIdGenerator.Object,
                logger.Object,
                _mockHttpContextAccessor.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_TemplateExists()
        {
            var original = ExamTemplateTestData.GetDraftTemplate();
            original.TemplateParts.Add(new TemplatePart
            {
                TemplatePartId = "PART_OLD",
                PartTitle = "Part 1",
                QuestionFrom = 1,
                QuestionTo = 10
            });

            var command = new DuplicateExamTemplateCommand(original.ExamTemplateId);

            _mockExamTemplateRepo.Setup(x => x.GetByIdWithPartsAsync(original.ExamTemplateId, It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(original);

            _mockExamTemplateRepo.Setup(x => x.IsNameExistsAsync(It.IsAny<string>(), null))
                                 .ReturnsAsync(false);

            _mockIdGenerator.Setup(x => x.GenerateCustom(10)).Returns("NEW_ID");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("NEW_ID");

            _mockExamTemplateRepo.Verify(x => x.AddAsync(It.Is<ExamTemplate>(e =>
                e.ExamTemplateId == "NEW_ID" &&
                e.CreatedBy == "TEST_USER_ID"
            )), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TemplateNotFound()
        {
            _mockExamTemplateRepo.Setup(x => x.GetByIdWithPartsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                 .ReturnsAsync((ExamTemplate?)null);

            var command = new DuplicateExamTemplateCommand("INVALID_ID");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(AppErrors.ExamTemplateNotFound);
        }
    }
}