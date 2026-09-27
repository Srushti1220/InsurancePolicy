using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text.Json;
using System.Threading.Tasks;

namespace InsuranceAPI.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = HttpStatusCode.InternalServerError;
            var errorCode = "INTERNAL_SERVER_ERROR";
            var message = "An unexpected error occurred on the server.";

            switch (exception)
            {
                case KeyNotFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    errorCode = "NOT_FOUND";
                    message = exception.Message;
                    break;

                case ValidationException validationEx:
                    statusCode = HttpStatusCode.BadRequest;
                    errorCode = "VALIDATION_ERROR";
                    var errors = validationEx.Errors?.Select(e => e.ErrorMessage).ToList();
                    message = errors != null && errors.Any() 
                        ? string.Join("; ", errors) 
                        : validationEx.Message;
                    break;

                case InvalidOperationException:
                    if (exception.Message.Contains("already", StringComparison.OrdinalIgnoreCase) || 
                        exception.Message.Contains("taken", StringComparison.OrdinalIgnoreCase) ||
                        exception.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                    {
                        statusCode = HttpStatusCode.Conflict;
                        errorCode = "CONFLICT";
                    }
                    else
                    {
                        statusCode = HttpStatusCode.BadRequest;
                        errorCode = "BAD_REQUEST";
                    }
                    message = exception.Message;
                    break;

                case UnauthorizedAccessException:
                    statusCode = HttpStatusCode.Unauthorized;
                    errorCode = "UNAUTHORIZED";
                    message = exception.Message;
                    break;

                case SecurityException:
                    statusCode = HttpStatusCode.Forbidden;
                    errorCode = "FORBIDDEN";
                    message = exception.Message;
                    break;

                case ArgumentException:
                    statusCode = HttpStatusCode.BadRequest;
                    errorCode = "BAD_REQUEST";
                    message = exception.Message;
                    break;

                default:
                    message = exception.Message;
                    break;
            }

            // Log exception: Error for 500s or general unhandled, Warning for handled business rejections
            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
            }
            else
            {
                _logger.LogWarning("Handled application exception [{ErrorCode}] ({StatusCode}): {Message}", errorCode, (int)statusCode, message);
            }

            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                success = false,
                message = message,
                errorCode = errorCode,
                timestamp = DateTime.UtcNow.ToString("o"),
                statusCode = (int)statusCode
            };

            var options = new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            };
            return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}
