// =======================
// FAVORITE VOCABULARY TEST BASE
// Dùng CHUNG cho:
// - AddFavoriteVocabulary
// - RemoveFavoriteVocabulary
// - GetFavoriteVocabularies
// =======================
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;

namespace Tokki.UnitTests.Common.Bases
{
    public abstract class FavoriteVocabularyTestBase
    {
        protected readonly Mock<IUserFavoriteVocabularyRepository> _mockFavoriteRepo;
        protected readonly Mock<IVocabularyRepository> _mockVocabularyRepo;
        protected readonly Mock<ITopicRepository> _mockTopicRepo;
        protected readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        protected readonly Mock<IIdGeneratorService> _mockIdGenerator;

        protected const string DefaultUserId = "user-test-01";

        protected FavoriteVocabularyTestBase()
        {
            _mockFavoriteRepo = new Mock<IUserFavoriteVocabularyRepository>();
            _mockVocabularyRepo = new Mock<IVocabularyRepository>();
            _mockTopicRepo = new Mock<ITopicRepository>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockIdGenerator = new Mock<IIdGeneratorService>();

            SetupAuthenticatedUser(DefaultUserId);
        }

        protected void SetupAuthenticatedUser(string userId)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var context = new DefaultHttpContext
            {
                User = principal
            };

            _mockHttpContextAccessor
                .Setup(x => x.HttpContext)
                .Returns(context);
        }

        protected void SetupUnauthenticatedUser()
        {
            _mockHttpContextAccessor
                .Setup(x => x.HttpContext)
                .Returns(new DefaultHttpContext());
        }
    }
}
