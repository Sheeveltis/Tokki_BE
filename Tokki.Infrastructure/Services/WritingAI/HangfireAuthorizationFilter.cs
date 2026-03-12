// Infrastructure/Configurations/HangfireAuthorizationFilter.cs
using Hangfire.Dashboard;

namespace Tokki.Infrastructure.Configurations
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // TODO: Add proper authentication (check if user is admin)
            // For development: allow all
            return true;
        }
    }
}