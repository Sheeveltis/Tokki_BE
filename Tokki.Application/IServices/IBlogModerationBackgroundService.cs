using System.Threading.Tasks;

namespace Tokki.Application.IServices
{
    public interface IBlogModerationBackgroundService
    {
        Task ModerateBlogAsync(string blogId);
        Task ModerateAdminBlogAsync(string blogId);
    }
}
