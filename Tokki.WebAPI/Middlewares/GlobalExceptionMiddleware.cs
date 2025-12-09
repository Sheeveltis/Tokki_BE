using System.Net;
using System.Text.Json;
using FluentValidation;
using Tokki.Application.Common.Models;

namespace Tokki.WebAPI.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
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
                "Đã xảy ra lỗi hệ thống."
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