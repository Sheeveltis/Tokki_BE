using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.EmailTemplates.Queries.GetEmailCampaigns;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Xunit;

namespace Tokki.UnitTests.Features.EmailTemplates.Queries
{
    public class GetEmailCampaignsQueryHandlerTests : EmailCampaignTestBase
    {
        private readonly GetEmailCampaignsQueryHandler _handler;

        public GetEmailCampaignsQueryHandlerTests()
        {
            _handler = new GetEmailCampaignsQueryHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnPagedResult()
        {
            var jobs = new List<EmailJob>
            {
                new EmailJob { JobId = "job-1", Status = EmailJobStatus.Pending },
                new EmailJob { JobId = "job-2", Status = EmailJobStatus.Pending }
            };

            _mockRepo.Setup(x => x.GetPagedAsync(
                    1,
                    10,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    false))
                .ReturnsAsync((jobs, 2));

            var query = new GetEmailCampaignsQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task Handle_Should_ReturnEmpty_When_NoData()
        {
            _mockRepo.Setup(x => x.GetPagedAsync(
                    1,
                    10,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    false))
                .ReturnsAsync((new List<EmailJob>(), 0));

            var query = new GetEmailCampaignsQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
        }
    }
}
