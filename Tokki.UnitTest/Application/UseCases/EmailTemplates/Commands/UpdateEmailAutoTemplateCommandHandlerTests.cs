using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailTemplate;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates.Commands
{
    public class UpdateEmailAutoTemplateCommandHandlerTests
    {
        private readonly Mock<IEmailTemplateRepository> _repoMock;

        public UpdateEmailAutoTemplateCommandHandlerTests()
        {
            _repoMock = new Mock<IEmailTemplateRepository>();
        }

        private UpdateEmailAutoTemplateCommandHandler CreateHandler()
        {
            return new UpdateEmailAutoTemplateCommandHandler(_repoMock.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-EMTA-UP-01 | A | Template Not Found -> Error
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TemplateNotFound_ShouldReturnError()
        {
            _repoMock.Setup(x => x.GetByIdAsync("fake-id")).ReturnsAsync((EmailTemplate?)null);
            var handler = CreateHandler();
            var cmd = new UpdateEmailAutoTemplateCommand { TemplateId = "fake-id" };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Code == AppErrors.EmailTemplateNotFound.Code);

            QACollector.LogTestCase("Email Template - Update Auto", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailAutoTemplateCommandHandler",
                TestCaseID = "TC-EMTA-UP-01",
                Description = "Returns EmailTemplateNotFound if ID not present",
                ExpectedResult = "Validation Error EmailTemplateNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Template = null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-EMTA-UP-02 | A | Name Duplication -> Error
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NameDuplicated_ShouldReturnError()
        {
            var template = new EmailTemplate { TemplateId = "1", TemplateName = "Old", Status = EmailTemplateStatus.Active };
            var existingOther = new EmailTemplate { TemplateId = "2", TemplateName = "New", Status = EmailTemplateStatus.Active };
            
            _repoMock.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(template);
            _repoMock.Setup(x => x.GetByNameAsync("New")).ReturnsAsync(existingOther);
            
            var handler = CreateHandler();
            var cmd = new UpdateEmailAutoTemplateCommand { TemplateId = "1", TemplateName = "New" };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Code == AppErrors.EmailTemplateKeyDuplicated.Code);

            QACollector.LogTestCase("Email Template - Update Auto", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailAutoTemplateCommandHandler",
                TestCaseID = "TC-EMTA-UP-02",
                Description = "Changing name to an existing Active template throws KeyDuplication",
                ExpectedResult = "KeyDuplicated Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "newTemplateName != oldName && existing is active" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-EMTA-UP-03 | A | Config Duplication -> Error
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ConfigDuplicated_ShouldReturnError()
        {
            var template = new EmailTemplate { TemplateId = "1", Type = EmailTemplateType.OfflineReminder, Value = 1, TargetGroup = UserTargetGroup.All };
            var existingOther = new EmailTemplate { TemplateId = "2", Status = EmailTemplateStatus.Active };
            
            _repoMock.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(template);
            _repoMock.Setup(x => x.GetByTypeValueTargetAsync(EmailTemplateType.VipExpiringReminder, 1, UserTargetGroup.All))
                     .ReturnsAsync(existingOther);
            
            var handler = CreateHandler();
            var cmd = new UpdateEmailAutoTemplateCommand { TemplateId = "1", Type = EmailTemplateType.VipExpiringReminder, Value = 1, TargetGroup = UserTargetGroup.All };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Code == AppErrors.EmailTemplateKeyDuplicated.Code);

            QACollector.LogTestCase("Email Template - Update Auto", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailAutoTemplateCommandHandler",
                TestCaseID = "TC-EMTA-UP-03",
                Description = "Changing enum composite keys overlapping with active template triggers error",
                ExpectedResult = "KeyDuplicated Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Config keys changed intersecting another" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-EMTA-UP-04 | N | No Changes made -> 200 Fast Return
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoChanges_ShouldReturn200WithoutSaving()
        {
            var template = new EmailTemplate { TemplateId = "1", TemplateName = "N", Subject = "S" };
            _repoMock.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(template);
            
            var handler = CreateHandler();
            var cmd = new UpdateEmailAutoTemplateCommand { TemplateId = "1", TemplateName = "N", Subject = "S", Body = null }; // Nothing changes

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Không có dữ liệu hợp lệ để cập nhật!");
            _repoMock.Verify(x => x.UpdateAsync(It.IsAny<EmailTemplate>()), Times.Never);

            QACollector.LogTestCase("Email Template - Update Auto", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailAutoTemplateCommandHandler",
                TestCaseID = "TC-EMTA-UP-04",
                Description = "If no fields changed, fast return true preventing DB hit",
                ExpectedResult = "Return 200 Not Changed",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "anyChanged = false" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-EMTA-UP-05 | N | Partially Updates Fields -> 200 Success
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PartialUpdates_ShouldSaveAndReturn200()
        {
            var template = new EmailTemplate { TemplateId = "1", Subject = "S", Body = "B", Description = "D", Status = EmailTemplateStatus.Active };
            _repoMock.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(template);
            
            var handler = CreateHandler();
            var cmd = new UpdateEmailAutoTemplateCommand { TemplateId = "1", Subject = "NewS", Body = "NewB", Description = "NewD", Status = EmailTemplateStatus.Draft };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            template.Subject.Should().Be("NewS");
            template.Status.Should().Be(EmailTemplateStatus.Draft);
            
            _repoMock.Verify(x => x.UpdateAsync(template), Times.Once);

            QACollector.LogTestCase("Email Template - Update Auto", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailAutoTemplateCommandHandler",
                TestCaseID = "TC-EMTA-UP-05",
                Description = "String properties update correctly if given different values",
                ExpectedResult = "Return 200 Updated",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Subject, status, body mutated" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-EMTA-UP-06 | N | Update duplicated Name but is same template -> 200 Success
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NameCheckSameId_ShouldPass()
        {
            var template = new EmailTemplate { TemplateId = "1", TemplateName = "Old", Status = EmailTemplateStatus.Active };
            
            // Name changes so it checks, but somehow it points to same template if it did match
            _repoMock.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(template);
            _repoMock.Setup(x => x.GetByNameAsync("New")).ReturnsAsync(template); // Same ID!
            
            var handler = CreateHandler();
            var cmd = new UpdateEmailAutoTemplateCommand { TemplateId = "1", TemplateName = "New" };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            template.TemplateName.Should().Be("New");

            QACollector.LogTestCase("Email Template - Update Auto", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailAutoTemplateCommandHandler",
                TestCaseID = "TC-EMTA-UP-06",
                Description = "Name duplicate check passes if existingByName has SAME id as current",
                ExpectedResult = "Return 200 bypass",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "existing.TemplateId == template.TemplateId" }
            });
        }
    }
}
