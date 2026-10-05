using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PersonalFinancialManagement.Application.Common.Exceptions;

namespace PersonalFinancialManagement.Api.Middleware;

// Turns exceptions thrown from the MediatR pipeline into ProblemDetails responses.
public class ExceptionHandlingMiddleware
{
    private const string ProblemContentType = "application/problem+json";

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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client went away; there is nobody to respond to.
            _logger.LogDebug("Request was cancelled by the client");
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        switch (exception)
        {
            case ValidationException validation:
                var errors = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

                await WriteAsync(context, new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred."
                });
                break;

            case NotFoundException:
                await WriteAsync(context, Problem(StatusCodes.Status404NotFound, "Resource not found.", exception.Message));
                break;

            case ConflictException:
                await WriteAsync(context, Problem(StatusCodes.Status409Conflict, "Conflict.", exception.Message));
                break;

            case BusinessRuleException:
                await WriteAsync(context, Problem(StatusCodes.Status400BadRequest, "The request could not be processed.", exception.Message));
                break;

            case UnauthorizedException:
                await WriteAsync(context, Problem(StatusCodes.Status401Unauthorized, "Unauthorized.", exception.Message));
                break;

            case ExternalServiceException:
                _logger.LogWarning(exception, "External service failure");
                await WriteAsync(context, Problem(StatusCodes.Status503ServiceUnavailable, "An external service is unavailable.", exception.Message));
                break;

            default:
                _logger.LogError(exception, "Unhandled exception");
                await WriteAsync(context, Problem(StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null));
                break;
        }
    }

    private static ProblemDetails Problem(int status, string title, string? detail)
        => new() { Status = status, Title = title, Detail = detail };

    private static Task WriteAsync(HttpContext context, ProblemDetails problem)
    {
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        // Serialize by the runtime type, otherwise ValidationProblemDetails.Errors would be dropped.
        return context.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, contentType: ProblemContentType);
    }
}
