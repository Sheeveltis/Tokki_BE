using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;

namespace Tokki.UnitTests.Common.Bases
{
    #region Test Base
    public class WordTestBase
    {
        protected readonly Mock<IWordRepository> _mockWordRepo;
        protected readonly Mock<IMeaningRepository> _mockMeaningRepo;
        protected readonly Mock<ITopicRepository> _mockTopicRepo;
        protected readonly Mock<IMeaningTopicRepository> _mockMeaningTopicRepo;
        protected readonly Mock<IIdGeneratorService> _mockIdGen;
        protected readonly Mock<ITextToSpeechService> _mockTtsService;
        protected readonly Mock<ICloudinaryService> _mockCloudinaryService;
        protected readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        protected readonly string _testUserId = "USER-TEST-123";

        public WordTestBase()
        {
            _mockWordRepo = new Mock<IWordRepository>();
            _mockMeaningRepo = new Mock<IMeaningRepository>();
            _mockTopicRepo = new Mock<ITopicRepository>();
            _mockMeaningTopicRepo = new Mock<IMeaningTopicRepository>();
            _mockIdGen = new Mock<IIdGeneratorService>();
            _mockTtsService = new Mock<ITextToSpeechService>();
            _mockCloudinaryService = new Mock<ICloudinaryService>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            SetupHttpContext();
        }

        protected void SetupHttpContext()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _testUserId)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        }
    }
}
    #endregion
