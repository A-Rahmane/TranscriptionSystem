using System.Net;
using System.Text.Json;
using FluentValidation;
using TranscriptionService.Domain.Exceptions;

namespace TranscriptionService.API.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse();

        switch (exception)
        {
            case ValidationException validationException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.StatusCode = response.StatusCode;
                errorResponse.Message = "Validation failed";
                errorResponse.Errors = validationException.Errors
                    .Select(e => new ErrorDetail
                    {
                        Field = e.PropertyName,
                        Message = e.ErrorMessage
                    })
                    .ToList();
                break;

            case TranscriptionJobNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.StatusCode = response.StatusCode;
                errorResponse.Message = exception.Message;
                break;

            case InvalidAudioFileException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.StatusCode = response.StatusCode;
                errorResponse.Message = exception.Message;
                break;

            case DomainException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.StatusCode = response.StatusCode;
                errorResponse.Message = exception.Message;
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.StatusCode = response.StatusCode;
                errorResponse.Message = "Unauthorized access";
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.StatusCode = response.StatusCode;
                errorResponse.Message = "An internal server error occurred";
                break;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var result = JsonSerializer.Serialize(errorResponse, jsonOptions);
        return response.WriteAsync(result);
    }

    private class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ErrorDetail> Errors { get; set; } = new();
    }

    private class ErrorDetail
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
