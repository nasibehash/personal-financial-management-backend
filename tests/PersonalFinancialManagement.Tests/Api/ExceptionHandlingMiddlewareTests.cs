using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PersonalFinancialManagement.Api.Middleware;
using PersonalFinancialManagement.Application.Common.Exceptions;

namespace PersonalFinancialManagement.Tests.Api;

public class ExceptionHandlingMiddlewareTests
{
    private sealed record Response(int Status, string? ContentType, JsonElement Body);

    private static async Task<Response> Run(Exception exception)
    {
        var context = new DefaultHttpContext { RequestServices = new ServiceCollection().BuildServiceProvider() };
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(_ => throw exception, NullLogger<ExceptionHandlingMiddleware>.Instance);
        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return new Response(context.Response.StatusCode, context.Response.ContentType, document.RootElement.Clone());
    }

    [Fact]
    public async Task Validation_failures_are_listed_per_field()
    {
        var failures = new[]
        {
            new ValidationFailure("Name", "'Name' must not be empty."),
            new ValidationFailure("Name", "'Name' must not be empty."),
            new ValidationFailure("Amount", "'Amount' must be greater than 0.")
        };

        var response = await Run(new ValidationException(failures));

        Assert.Equal(400, response.Status);
        Assert.StartsWith("application/problem+json", response.ContentType);
        var errors = response.Body.GetProperty("errors");
        Assert.Equal(["'Name' must not be empty."], errors.GetProperty("Name").EnumerateArray().Select(e => e.GetString())); // duplicates collapsed
        Assert.Equal(["'Amount' must be greater than 0."], errors.GetProperty("Amount").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(400, response.Body.GetProperty("status").GetInt32());
    }

    [Theory]
    [InlineData(typeof(NotFoundException), 404)]
    [InlineData(typeof(ConflictException), 409)]
    [InlineData(typeof(BusinessRuleException), 400)]
    [InlineData(typeof(UnauthorizedException), 401)]
    [InlineData(typeof(ExternalServiceException), 503)]
    public async Task Application_exceptions_map_to_their_status_codes(Type exceptionType, int expectedStatus)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Something specific happened")!;

        var response = await Run(exception);

        Assert.Equal(expectedStatus, response.Status);
        Assert.StartsWith("application/problem+json", response.ContentType);
        Assert.Equal(expectedStatus, response.Body.GetProperty("status").GetInt32());
        Assert.Equal("Something specific happened", response.Body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Unexpected_errors_do_not_leak_their_message()
    {
        var response = await Run(new InvalidOperationException("connection string: Server=secret;Password=hunter2"));

        Assert.Equal(500, response.Status);
        Assert.DoesNotContain("hunter2", response.Body.GetRawText());
        Assert.False(response.Body.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String);
    }

    [Fact]
    public async Task A_request_cancelled_by_the_client_is_not_answered()
    {
        using var cancellation = new CancellationTokenSource();
        var context = new DefaultHttpContext { RequestServices = new ServiceCollection().BuildServiceProvider() };
        context.Response.Body = new MemoryStream();
        context.RequestAborted = cancellation.Token;
        cancellation.Cancel();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new OperationCanceledException(cancellation.Token),
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        await middleware.InvokeAsync(context);

        Assert.Equal(0, context.Response.Body.Length);
    }
}
