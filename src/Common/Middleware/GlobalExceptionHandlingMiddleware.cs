using System.Net;
using System.Text.Json;
using Common.Exceptions;
using Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Common.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case BaseException baseEx:
                response.Title = "Application Error";
                response.Status = baseEx.StatusCode;
                response.Detail = baseEx.Message;
                response.ErrorCode = baseEx.ErrorCode;
                response.AdditionalData = baseEx.AdditionalData;
                context.Response.StatusCode = baseEx.StatusCode;
                
                if (baseEx.StatusCode >= 500)
                {
                    _logger.LogError(baseEx, "Application error occurred: {ErrorCode}", baseEx.ErrorCode);
                }
                else
                {
                    _logger.LogWarning(baseEx, "Client error occurred: {ErrorCode}", baseEx.ErrorCode);
                }
                break;

            case TaskCanceledException:
                response.Title = "Request Timeout";
                response.Status = (int)HttpStatusCode.RequestTimeout;
                response.Detail = "The request was cancelled or timed out.";
                response.ErrorCode = "REQUEST_TIMEOUT";
                context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                _logger.LogWarning("Request timeout occurred");
                break;

            case UnauthorizedAccessException:
                response.Title = "Unauthorized";
                response.Status = (int)HttpStatusCode.Unauthorized;
                response.Detail = "You are not authorized to access this resource.";
                response.ErrorCode = "UNAUTHORIZED";
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                _logger.LogWarning("Unauthorized access attempt");
                break;

            case ArgumentException argEx:
                response.Title = "Bad Request";
                response.Status = (int)HttpStatusCode.BadRequest;
                response.Detail = argEx.Message;
                response.ErrorCode = "INVALID_ARGUMENT";
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                _logger.LogWarning(argEx, "Invalid argument provided");
                break;

            case KeyNotFoundException:
                response.Title = "Not Found";
                response.Status = (int)HttpStatusCode.NotFound;
                response.Detail = "The requested resource was not found.";
                response.ErrorCode = "NOT_FOUND";
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                _logger.LogWarning("Resource not found");
                break;

            case InvalidOperationException invalidOpEx:
                response.Title = "Invalid Operation";
                response.Status = (int)HttpStatusCode.BadRequest;
                response.Detail = invalidOpEx.Message;
                response.ErrorCode = "INVALID_OPERATION";
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                _logger.LogWarning(invalidOpEx, "Invalid operation attempted");
                break;

            default:
                response.Title = "Internal Server Error";
                response.Status = (int)HttpStatusCode.InternalServerError;
                response.Detail = "An unexpected error occurred. Please try again later.";
                response.ErrorCode = "INTERNAL_ERROR";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                _logger.LogError(exception, "Unhandled exception occurred");
                break;
        }

        // Add trace identifier for debugging
        response.TraceId = context.TraceIdentifier;
        
        // Add timestamp
        response.Timestamp = DateTime.UtcNow;

        var jsonResponse = JsonSerializer.Serialize(response, _jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }
}

public class ErrorResponse
{
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public IDictionary<string, object>? AdditionalData { get; set; }
}