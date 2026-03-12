using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Reflection;
using Tokki.Application.Common.Behaviors;
using Tokki.Application.Common.Helpers;
using Tokki.Application.Common.Helpers.ValidationVietnameseLanguageManager;

namespace Tokki.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = typeof(DependencyInjection).Assembly;

            ValidatorOptions.Global.LanguageManager = new ValidationVietnameseLanguageManager();
            ValidatorOptions.Global.LanguageManager.Enabled = true;
            ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("vi");
            services.AddScoped<EmailNotificationHelper>();
            services.AddMediatR(configuration =>
            {
                configuration.RegisterServicesFromAssembly(assembly);
                configuration.AddOpenBehavior(typeof(Common.Behaviors.ValidationBehavior<,>));
            });
            services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

            services.AddValidatorsFromAssembly(assembly);

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.Behaviors.ValidationBehavior<,>));

            return services;
        }
    }
}