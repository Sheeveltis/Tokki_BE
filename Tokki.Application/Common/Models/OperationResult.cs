using System.Text.Json.Serialization;

namespace Tokki.Application.Common.Models
{
    public class OperationResult<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public List<Error>? Errors { get; set; }
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; } = 200;

        private OperationResult() { }

        public static OperationResult<T> Success(T data, int statusCode = 200, string message = "Thành công")
        {
            return new OperationResult<T>
            {
                IsSuccess = true,
                Data = data,
                Errors = null,
                StatusCode = statusCode,
                Message = message
            };
        }

        public static OperationResult<T> Failure(Error error, int statusCode, string uiMessage)
        {
            return new OperationResult<T>
            {
                IsSuccess = false,
                Data = default,
                Errors = new List<Error> { error },
                StatusCode = statusCode,
                Message = uiMessage
            };
        }

        public static OperationResult<T> Failure(List<Error> errors, int statusCode = 400, string uiMessage = "Thất bại")
        {
            return new OperationResult<T>
            {
                IsSuccess = false,
                Data = default,
                Errors = errors,
                StatusCode = statusCode,
                Message = uiMessage
            };
        }

        public static OperationResult<T> Failure(string errorMsg, int statusCode = 400)
        {
            return Failure(new Error("Error.Generic", errorMsg), statusCode, errorMsg);
        }
    }
}