using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.EmailTemplates.Queries.GetEmailCampaignById;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Xunit;

namespace Tokki.UnitTests.Features.EmailTemplates.Queries
{
    public class GetEmailCampaignByIdQueryHandlerTests : EmailCampaignTestBase
    {
        private readonly GetEmailCampaignByIdQueryHandler _handler;

        public GetEmailCampaignByIdQueryHandlerTests()
        {
            _handler = new GetEmailCampaignByIdQueryHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_Found()
        {
            var job = new EmailJob { JobId = "job-01" };

            _mockRepo.Setup(x => x.GetByIdAsync(job.JobId))
                     .ReturnsAsync(job);

            var query = new GetEmailCampaignByIdQuery
            {
                JobId = job.JobId
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be(job);

            _mockRepo.Verify(x => x.GetByIdAsync(job.JobId), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_NotFound()
        {
            var query = new GetEmailCampaignByIdQuery
            {
                JobId = "not-found"
            };

            _mockRepo.Setup(x => x.GetByIdAsync(query.JobId))
                     .ReturnsAsync((EmailJob?)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }
    }
}
