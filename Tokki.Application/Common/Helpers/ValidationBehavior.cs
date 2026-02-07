using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.Common.Helpers
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Nếu không có validator nào, tiếp tục xử lý bình thường
            if (!_validators.Any())
            {
                return await next();
            }

            // Tạo context để validate
            var context = new ValidationContext<TRequest>(request);

            // Chạy tất cả validators
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            // Lấy tất cả lỗi
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            // Nếu có lỗi, trả về OperationResult.Failure
            if (failures.Any())
            {
                var errors = failures.Select(f => new Error(
                    "Validation.Error",
                    f.ErrorMessage
                )).ToList();

                var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));

                // Kiểm tra TResponse có phải OperationResult<T> không
                var responseType = typeof(TResponse);

                if (responseType.IsGenericType &&
                    responseType.GetGenericTypeDefinition() == typeof(OperationResult<>))
                {
                    // Lấy kiểu dữ liệu T trong OperationResult<T>
                    var dataType = responseType.GetGenericArguments()[0];

                    // Gọi phương thức Failure của OperationResult<T>
                    // Sử dụng overload: Failure(List<Error> errors, int statusCode, string uiMessage)
                    var failureMethod = typeof(OperationResult<>)
                        .MakeGenericType(dataType)
                        .GetMethod("Failure", new[] { typeof(List<Error>), typeof(int), typeof(string) });

                    if (failureMethod != null)
                    {
                        var result = failureMethod.Invoke(null, new object[] { errors, 400, errorMessage });
                        return (TResponse)result!;
                    }
                }
            }

            // Không có lỗi, tiếp tục xử lý
            return await next();
        }
    }
}