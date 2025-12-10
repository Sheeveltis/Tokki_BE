using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.EmailTemplates.Queries;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.EmailTemplates.Queries
{
    public class GetAllEmailTemplatesQueryHandlerTests : EmailTemplateTestBase
    {
        private readonly GetAllEmailTemplatesQueryHandler _handler;

        public GetAllEmailTemplatesQueryHandlerTests()
        {
            _handler = new GetAllEmailTemplatesQueryHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnList_When_Called()
        {
            // 1. Arrange
            var query = EmailTemplateTestData.GetGetAllQuery();
            var fakeList = EmailTemplateTestData.GetFakeEmailTemplateList();

            // Giả lập Repository trả về danh sách 3 phần tử
            _mockRepo.Setup(x => x.GetAllAsync())
                     .ReturnsAsync(fakeList);

            // 2. Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // 3. Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // Kiểm tra dữ liệu trả về
            result.Data.Should().NotBeNull();
            result.Data.Count.Should().Be(3);
            result.Data[0].TemplateKey.Should().Be("KEY_1");

            // Verify
            _mockRepo.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnEmptyList_When_NoData()
        {
            // 1. Arrange
            _mockRepo.Setup(x => x.GetAllAsync())
                     .ReturnsAsync(new List<Domain.Entities.EmailTemplate>());

            // 2. Act
            var result = await _handler.Handle(new GetAllEmailTemplatesQuery(), CancellationToken.None);

            // 3. Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty(); // Phải là list rỗng, không được null
        }
    }
}