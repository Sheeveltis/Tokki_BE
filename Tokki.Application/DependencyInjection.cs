using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Reflection;
using Tokki.Application.Common.Helpers;

namespace Tokki.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = typeof(DependencyInjection).Assembly;

            // Cấu hình FluentValidation Language Manager
            ValidatorOptions.Global.LanguageManager = new ValidationVietnameseLanguageManager();
            ValidatorOptions.Global.LanguageManager.Enabled = true;
            ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("vi");

            // Đăng ký MediatR
            services.AddMediatR(configuration =>
                configuration.RegisterServicesFromAssembly(assembly));

            // Đăng ký AutoMapper
            services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

            // Đăng ký tất cả Validators
            services.AddValidatorsFromAssembly(assembly);

            // QUAN TRỌNG: Đăng ký ValidationBehavior để tự động validate
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            return services;
        }
    }
}