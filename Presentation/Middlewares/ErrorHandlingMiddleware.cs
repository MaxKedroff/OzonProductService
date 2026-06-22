using Application.Exceptions;
using Domain.Exceptions;
using System.Text.Json;

namespace Presentation.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
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

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new ErrorResponse
            {
                Path = context.Request.Path,
                Timestamp = DateTime.UtcNow
            };

            switch (exception)
            {
                case ValidationException validationEx:
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    errorResponse.StatusCode = 400;
                    errorResponse.Message = "Validation failed";
                    errorResponse.Errors = validationEx.Errors;
                    break;

                case NotFoundException notFoundEx:
                    response.StatusCode = StatusCodes.Status404NotFound;
                    errorResponse.StatusCode = 404;
                    errorResponse.Message = notFoundEx.Message;
                    break;

                case BusinessRuleException businessEx:
                    response.StatusCode = StatusCodes.Status409Conflict;
                    errorResponse.StatusCode = 409;
                    errorResponse.Message = businessEx.Message;
                    break;

                case DomainException domainEx:
                    response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                    errorResponse.StatusCode = 422;
                    errorResponse.Message = domainEx.Message;
                    break;

                case FluentValidation.ValidationException fluentEx:
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    errorResponse.StatusCode = 400;
                    errorResponse.Message = "Validation failed";
                    errorResponse.Errors = fluentEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        );
                    break;

                default:
                    response.StatusCode = StatusCodes.Status500InternalServerError;
                    errorResponse.StatusCode = 500;
                    errorResponse.Message = "An unexpected error occurred";
                    _logger.LogError(exception, "Unhandled exception occurred");
                    break;
            }

            var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            await response.WriteAsync(json);
        }
    }

    public record ErrorResponse
    {
        public int StatusCode { get; set; }
        public string? Message { get; set; }
        public string? Path { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
