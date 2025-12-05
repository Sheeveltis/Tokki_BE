using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Infrastructure.Data;
using Tokki.Infrastructure.Repositories;
using Tokki.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
namespace Tokki.Infrastructure
{
    public static class DependencyInjection
    {
        // Hàm này sẽ được gọi bên WebAPI
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Đăng ký DbContext
            services.AddDbContext<TokkiDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // 2. Đăng ký Repositories
            services.AddScoped<IBlogRepository, BlogRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            // 3. Đăng ký các Services khác (IdGenerator, Email, Storage...)
            services.AddSingleton<IIdGeneratorService, IdGeneratorService>();
            services.AddScoped<ISePayService, SePayService>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            return services;
        }
    }
}
