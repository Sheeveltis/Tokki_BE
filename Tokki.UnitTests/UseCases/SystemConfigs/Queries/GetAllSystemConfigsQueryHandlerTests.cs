using FluentAssertions;
using Moq;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.SystemConfigs.Queries.GetAll;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.SystemConfigs.Queries
{
    public class GetAllSystemConfigsQueryHandlerTests : SystemConfigTestBase
    {
        private readonly GetAllSystemConfigsQueryHandler _handler;

        public GetAllSystemConfigsQueryHandlerTests()
        {
            _handler = new GetAllSystemConfigsQueryHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnPagedResult_When_Valid()
        {
            // Arrange
            var query = SystemConfigTestData.BuildGetAllQuery(pageNumber: 2, pageSize: 3);

            var entities = SystemConfigTestData.BuildEntities(3);
            var totalCount = 10;

            _mockRepo
                .Setup(x => x.GetPagedAsync(2, 3))
                .ReturnsAsync((entities, totalCount));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Get the list successfully");

            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(3);
            result.Data.TotalCount.Should().Be(10);
            result.Data.PageNumber.Should().Be(2);
            result.Data.PageSize.Should().Be(3);

            var first = result.Data.Items.First();
            first.Key.Should().Be("CFG_01");
        }
    }
}
