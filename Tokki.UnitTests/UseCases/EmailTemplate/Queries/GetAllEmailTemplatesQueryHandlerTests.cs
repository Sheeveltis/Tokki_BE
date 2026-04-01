using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.EmailTemplates.Queries.GetAllEmailTemplates;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Xunit;

namespace Tokki.UnitTests.Features.EmailTemplates.Queries
{
    public class GetAllEmailAutoTemplatesQueryHandlerTests : EmailTemplateTestBase
    {
        private readonly GetAllEmailAutoTemplatesQueryHandler _handler;

        public GetAllEmailAutoTemplatesQueryHandlerTests()
        {
            _handler = new GetAllEmailAutoTemplatesQueryHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnPagedResult_And_ExcludeDeleted_ByDefault()
        {
            // Arrange
            var now = DateTime.UtcNow.AddHours(7);

            var templates = new List<EmailTemplate>
            {
                new EmailTemplate
                {
                    TemplateId = "tpl-1",
                    TemplateName = "Welcome",
                    Subject = "Hello",
                    Status = EmailTemplateStatus.Active,
                    Type = EmailTemplateType.OfflineReminder,
                    TargetGroup = UserTargetGroup.All,
                    Value = 7,
                    CreateAt = now.AddDays(-1)
                },
                new EmailTemplate
                {
                    TemplateId = "tpl-2",
                    TemplateName = "VIP Reminder",
                    Subject = "VIP",
                    Status = EmailTemplateStatus.Deleted, // phải bị loại khi không truyền Status
                    Type = EmailTemplateType.VipExpiringReminder,
                    TargetGroup = UserTargetGroup.VipUsers,
                    Value = 3,
                    CreateAt = now
                },
                new EmailTemplate
                {
                    TemplateId = "tpl-3",
                    TemplateName = "Offline 2",
                    Subject = "Offline",
                    Status = EmailTemplateStatus.Draft,
                    Type = EmailTemplateType.OfflineReminder,
                    TargetGroup = UserTargetGroup.FreeUsers,
                    Value = 10,
                    CreateAt = now.AddDays(-2)
                }
            };

            _mockRepo.Setup(x => x.GetPagedAsync(1, int.MaxValue))
                     .ReturnsAsync((templates, templates.Count));

            var query = new GetAllEmailAutoTemplatesQuery
            {
                PageNumber = 1,
                PageSize = 10
                // không truyền Status => mặc định exclude Deleted
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Retrieve list of email templates successfully");

            result.Data.Should().NotBeNull();
            result.Data.Items.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);

            result.Data.Items.Any(x => x.Status == EmailTemplateStatus.Deleted).Should().BeFalse();

            _mockRepo.Verify(x => x.GetPagedAsync(1, int.MaxValue), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_IncludeDeleted_When_StatusFilterIsDeleted()
        {
            // Arrange
            var now = DateTime.UtcNow.AddHours(7);

            var templates = new List<EmailTemplate>
            {
                new EmailTemplate { TemplateId = "tpl-a", Status = EmailTemplateStatus.Active, CreateAt = now.AddDays(-1) },
                new EmailTemplate { TemplateId = "tpl-d", Status = EmailTemplateStatus.Deleted, CreateAt = now }
            };

            _mockRepo.Setup(x => x.GetPagedAsync(1, int.MaxValue))
                     .ReturnsAsync((templates, templates.Count));

            var query = new GetAllEmailAutoTemplatesQuery
            {
                Status = EmailTemplateStatus.Deleted,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items[0].TemplateId.Should().Be("tpl-d");
            result.Data.Items[0].Status.Should().Be(EmailTemplateStatus.Deleted);
        }

        [Fact]
        public async Task Handle_Should_Filter_By_Type_TargetGroup_Value()
        {
            // Arrange
            var now = DateTime.UtcNow.AddHours(7);

            var templates = new List<EmailTemplate>
            {
                new EmailTemplate
                {
                    TemplateId = "tpl-1",
                    TemplateName = "A",
                    Status = EmailTemplateStatus.Active,
                    Type = EmailTemplateType.OfflineReminder,
                    TargetGroup = UserTargetGroup.All,
                    Value = 7,
                    CreateAt = now.AddDays(-1)
                },
                new EmailTemplate
                {
                    TemplateId = "tpl-2",
                    TemplateName = "B",
                    Status = EmailTemplateStatus.Active,
                    Type = EmailTemplateType.VipExpiringReminder,
                    TargetGroup = UserTargetGroup.VipUsers,
                    Value = 7,
                    CreateAt = now
                },
                new EmailTemplate
                {
                    TemplateId = "tpl-3",
                    TemplateName = "C",
                    Status = EmailTemplateStatus.Draft,
                    Type = EmailTemplateType.OfflineReminder,
                    TargetGroup = UserTargetGroup.All,
                    Value = 10,
                    CreateAt = now.AddDays(-2)
                }
            };

            _mockRepo.Setup(x => x.GetPagedAsync(1, int.MaxValue))
                     .ReturnsAsync((templates, templates.Count));

            var query = new GetAllEmailAutoTemplatesQuery
            {
                Type = EmailTemplateType.OfflineReminder,
                TargetGroup = UserTargetGroup.All,
                Value = 7,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items[0].TemplateId.Should().Be("tpl-1");
        }

        [Fact]
        public async Task Handle_Should_Search_By_Name_And_Subject_CaseInsensitive()
        {
            // Arrange
            var now = DateTime.UtcNow.AddHours(7);

            var templates = new List<EmailTemplate>
            {
                new EmailTemplate
                {
                    TemplateId = "tpl-1",
                    TemplateName = "Welcome User",
                    Subject = "Hello Tokki",
                    Status = EmailTemplateStatus.Active,
                    CreateAt = now.AddDays(-1)
                },
                new EmailTemplate
                {
                    TemplateId = "tpl-2",
                    TemplateName = "VIP Reminder",
                    Subject = "VIP EXPIRING",
                    Status = EmailTemplateStatus.Active,
                    CreateAt = now
                }
            };

            _mockRepo.Setup(x => x.GetPagedAsync(1, int.MaxValue))
                     .ReturnsAsync((templates, templates.Count));

            var query = new GetAllEmailAutoTemplatesQuery
            {
                SearchName = "welcome",
                SearchSubject = "tokki",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items[0].TemplateId.Should().Be("tpl-1");
        }

        [Fact]
        public async Task Handle_Should_Apply_Pagination_And_Sort_By_CreateAt_Desc()
        {
            // Arrange
            var now = DateTime.UtcNow.AddHours(7);

            var templates = new List<EmailTemplate>
            {
                new EmailTemplate { TemplateId = "tpl-1", Status = EmailTemplateStatus.Active, CreateAt = now.AddDays(-3) },
                new EmailTemplate { TemplateId = "tpl-2", Status = EmailTemplateStatus.Active, CreateAt = now.AddDays(-2) },
                new EmailTemplate { TemplateId = "tpl-3", Status = EmailTemplateStatus.Active, CreateAt = now.AddDays(-1) },
                new EmailTemplate { TemplateId = "tpl-4", Status = EmailTemplateStatus.Active, CreateAt = now }
            };

            _mockRepo.Setup(x => x.GetPagedAsync(1, int.MaxValue))
                     .ReturnsAsync((templates, templates.Count));

            var query = new GetAllEmailAutoTemplatesQuery
            {
                PageNumber = 2,
                PageSize = 2
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            // sort desc => [tpl-4, tpl-3, tpl-2, tpl-1]
            // page 2 size 2 => [tpl-2, tpl-1]
            result.Data.Items.Should().HaveCount(2);
            result.Data.Items[0].TemplateId.Should().Be("tpl-2");
            result.Data.Items[1].TemplateId.Should().Be("tpl-1");

            result.Data.TotalCount.Should().Be(4);
            result.Data.PageNumber.Should().Be(2);
            result.Data.PageSize.Should().Be(2);
        }

        [Fact]
        public async Task Handle_Should_ReturnEmpty_When_NoData()
        {
            // Arrange
            _mockRepo.Setup(x => x.GetPagedAsync(1, int.MaxValue))
                     .ReturnsAsync((new List<EmailTemplate>(), 0));

            var query = new GetAllEmailAutoTemplatesQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().NotBeNull();
            result.Data.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
        }
    }
}
