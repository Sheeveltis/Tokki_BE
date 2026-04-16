using System.Net;
using System.Text.Json;
using FluentValidation;
using Tokki.Application.Common.Models;

namespace Tokki.WebAPI.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = (int)HttpStatusCode.InternalServerError;
            var response = OperationResult<string>.Failure(
                AppErrors.ServerError,
                statusCode,
                AppErrors.ServerError.Description
            );

            if (exception is ValidationException validationEx)
            {
                statusCode = (int)HttpStatusCode.BadRequest; 

                var errors = validationEx.Errors
                    .Select(e => new Error(e.ErrorCode, e.ErrorMessage))
                    .ToList();

                var firstMessage = errors.FirstOrDefault()?.Description ?? "Dữ liệu không hợp lệ.";

                response = OperationResult<string>.Failure(errors, statusCode, firstMessage);
            }

            context.Response.StatusCode = statusCode;
            response.StatusCode = statusCode;

            var json = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(json);
        }
    }
}