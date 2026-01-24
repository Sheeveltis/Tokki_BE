using FluentAssertions;
using Moq;
using Tokki.Application.UseCases.ExamTemplates.Commands.CreateExamTemplate;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Microsoft.AspNetCore.Http;
using System.Security.Claims; 

namespace Tokki.UnitTests.Features.ExamTemplates.Commands
{
    public class CreateExamTemplateHandlerTests : ExamTemplateTestBase
    {
        private readonly CreateExamTemplateCommandHandler _handler;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor; 

        public CreateExamTemplateHandlerTests()
        {
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            var context = new DefaultHttpContext();
            var userId = "TEST_USER_ID";
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };

            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(context);
            _handler = new CreateExamTemplateCommandHandler(_mockExamTemplateRepo.Object, _mockIdGenerator.Object, _mockHttpContextAccessor.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_NameIsValid()
        {
            var command = ExamTemplateTestData.GetCreateCommand();
            _mockExamTemplateRepo.Setup(x => x.IsNameExistsAsync(command.Name, null))
                                 .ReturnsAsync(false);

            _mockIdGenerator.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns("NEW_ID");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("NEW_ID");

            _mockExamTemplateRepo.Verify(x => x.AddAsync(It.IsAny<ExamTemplate>()), Times.Once);
            _mockExamTemplateRepo.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_NameExists()
        {
            var command = ExamTemplateTestData.GetCreateCommand();
            _mockExamTemplateRepo.Setup(x => x.IsNameExistsAsync(command.Name, null))
                                 .ReturnsAsync(true);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Tên đề thi mẫu đã tồn tại."); 
            _mockExamTemplateRepo.Verify(x => x.AddAsync(It.IsAny<ExamTemplate>()), Times.Never);
        }
    }
}