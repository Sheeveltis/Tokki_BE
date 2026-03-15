using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailTemplate;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates
{
    public class CreateEmailAutoTemplateCommandHandlerTests
    {
        private CreateEmailAutoTemplateCommandHandler CreateHandler(
            Mock<IEmailTemplateRepository>? repo = null)
        {
            return new CreateEmailAutoTemplateCommandHandler(
                (repo ?? new Mock<IEmailTemplateRepository>()).Object,
                MockIdGeneratorService.GetMock().Object);
        }

        [Fact]
        public async Task Handle_DuplicateTemplateName_ShouldReturnFailure()
        {
            var command = new CreateEmailAutoTemplateCommand
            {
                TemplateName = "Chào mừng",
                Subject = "Xin chào",
                Body = "Body content"
            };

            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByNameAsync("Chào mừng"))
                    .ReturnsAsync(new EmailTemplate
                    {
                        TemplateName = "Chào mừng",
                        Status = EmailTemplateStatus.Draft
                    });

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("EmailTemplate - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Email Template",
                TestCaseID = "TC-ETPL-CRE-01",
                Description = "Tạo template với tên đã tồn tại (không phải Deleted)",
                ExpectedResult = "Return Failure EmailTemplateKeyDuplicated",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "TemplateName đã tồn tại",
                    "Status != Deleted",
                    "Return Failure"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidTemplate_ShouldReturn201WithTemplateId()
        {
            var command = new CreateEmailAutoTemplateCommand
            {
                TemplateName = "Template Mới",
                Subject = "Subject",
                Body = "Body"
            };

            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByNameAsync(It.IsAny<string>()))
                    .ReturnsAsync((EmailTemplate?)null);
            mockRepo.Setup(x => x.GetByTypeValueTargetAsync(
         It.IsAny<EmailTemplateType>(),
         It.IsAny<int>(),
         It.IsAny<UserTargetGroup>()))
     .ReturnsAsync((EmailTemplate?)null);
            mockRepo.Setup(x => x.AddAsync(It.IsAny<EmailTemplate>()))
                    .Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().NotBeNullOrEmpty();

            QACollector.LogTestCase("EmailTemplate - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Email Template",
                TestCaseID = "TC-ETPL-CRE-02",
                Description = "Tạo template hợp lệ → trả về TemplateId",
                ExpectedResult = "Return 201, Data = TemplateId",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid TemplateName",
                    "No duplicate",
                    "Return 201"
                }
            });
        }
    }
}