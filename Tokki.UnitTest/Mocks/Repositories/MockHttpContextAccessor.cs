using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace Tokki.UnitTest.Mocks.Services
{
    public static class MockHttpContextAccessor
    {
        /// <summary>
        /// Tạo IHttpContextAccessor có UserId hợp lệ trong Claims
        /// </summary>
        public static Mock<IHttpContextAccessor> GetMock(string userId = "USER-001")
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = principal };

            var mockAccessor = new Mock<IHttpContextAccessor>();
            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            return mockAccessor;
        }

        /// <summary>
        /// Tạo IHttpContextAccessor KHÔNG có UserId — giả lập unauthorized
        /// </summary>
        public static Mock<IHttpContextAccessor> GetUnauthorizedMock()
        {
            var mockAccessor = new Mock<IHttpContextAccessor>();
            mockAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
            return mockAccessor;
        }
    }
}