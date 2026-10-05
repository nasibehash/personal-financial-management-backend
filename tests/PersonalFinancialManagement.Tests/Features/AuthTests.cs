using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Features.Commands.LoginUser;
using PersonalFinancialManagement.Application.Features.Commands.RegisterUser;
using PersonalFinancialManagement.Application.Features.Queries.GetCurrentUser;
using PersonalFinancialManagement.Tests.Support;

namespace PersonalFinancialManagement.Tests.Features;

public class AuthTests
{
    [Fact]
    public async Task Register_creates_the_user_with_default_categories_and_a_token()
    {
        using var app = new TestApp();

        var result = await app.Send(new RegisterUserCommand("Sara Ahmadi", "  Sara@Example.com ", "Passw0rd123"));

        Assert.Equal("sara@example.com", result.User.Email);
        Assert.Equal("Sara Ahmadi", result.User.FullName);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));

        var categoryCount = await app.Query(db => db.Categories.CountAsync(c => c.UserId == result.User.Id));
        Assert.Equal(DefaultCategories.All.Count, categoryCount);
    }

    [Fact]
    public async Task Register_issues_a_token_for_the_new_user()
    {
        using var app = new TestApp();

        var result = await app.Send(new RegisterUserCommand("Sara Ahmadi", "sara@example.com", "Passw0rd123"));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal(result.User.Id.ToString(), token.Subject);
        Assert.Equal(result.ExpiresAtUtc, token.ValidTo, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Register_rejects_an_email_that_is_already_used()
    {
        using var app = new TestApp();
        await app.SignUp("sara@example.com");

        await Assert.ThrowsAsync<ConflictException>(
            () => app.Send(new RegisterUserCommand("Another Sara", "SARA@example.com", "Passw0rd123")));
    }

    [Theory]
    [InlineData("short1")]
    [InlineData("onlyletters")]
    [InlineData("12345678")]
    public async Task Register_rejects_weak_passwords(string password)
    {
        using var app = new TestApp();

        await Assert.ThrowsAsync<ValidationException>(
            () => app.Send(new RegisterUserCommand("Sara Ahmadi", "sara@example.com", password)));
    }

    [Fact]
    public async Task Register_rejects_an_invalid_email()
    {
        using var app = new TestApp();

        await Assert.ThrowsAsync<ValidationException>(
            () => app.Send(new RegisterUserCommand("Sara Ahmadi", "not-an-email", "Passw0rd123")));
    }

    [Fact]
    public async Task Login_succeeds_with_the_right_credentials()
    {
        using var app = new TestApp();
        var user = await app.SignUp("sara@example.com");

        var result = await app.Send(new LoginUserCommand("Sara@Example.com", "Passw0rd123"));

        Assert.Equal(user.Id, result.User.Id);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task Login_fails_with_a_wrong_password()
    {
        using var app = new TestApp();
        await app.SignUp("sara@example.com");

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => app.Send(new LoginUserCommand("sara@example.com", "WrongPassw0rd")));
    }

    [Fact]
    public async Task Login_fails_for_an_unknown_email()
    {
        using var app = new TestApp();

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => app.Send(new LoginUserCommand("nobody@example.com", "Passw0rd123")));
    }

    [Fact]
    public async Task GetCurrentUser_returns_the_signed_in_user()
    {
        using var app = new TestApp();
        var user = await app.SignUp("sara@example.com");

        var result = await app.Send(new GetCurrentUserQuery());

        Assert.Equal(user, result);
    }

    [Fact]
    public async Task GetCurrentUser_requires_authentication()
    {
        using var app = new TestApp();

        await Assert.ThrowsAsync<UnauthorizedException>(() => app.Send(new GetCurrentUserQuery()));
    }
}
