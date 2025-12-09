using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Tokki.Application.Common.Behaviors;
using Tokki.Application.Common.Helpers;

namespace Tokki.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = typeof(DependencyInjection).Assembly;
            ValidatorOptions.Global.LanguageManager = new ValidationVietnameseLanguageManager();

            services.AddMediatR(configuration =>
            {
                configuration.RegisterServicesFromAssembly(assembly);
                configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });
            services.AddAutoMapper(cfg => cfg.AddMaps(assembly));
            services.AddValidatorsFromAssembly(assembly);
            return services;
        }
    }
}