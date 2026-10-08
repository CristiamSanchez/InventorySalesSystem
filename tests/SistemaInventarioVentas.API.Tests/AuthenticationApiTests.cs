using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Infrastructure.Persistence;

namespace SistemaInventarioVentas.API.Tests;

public sealed class AuthenticationApiTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Register_CreatesInactiveUserWithPasswordHashAndNoCredentialDisclosure()
    {
        await fixture.ResetAsync();
        const string password = "Strong local test password";

        using var response = await fixture.Client.PostAsJsonAsync(
            "/auth/register",
            new { name = "Test User", email = "New.User@example.com", password });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(password, body, StringComparison.Ordinal);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);

        var registered = JsonSerializer.Deserialize<RegisterResponse>(body, JsonOptions);
        Assert.NotNull(registered);
        Assert.False(registered.IsActive);

        await using var dbContext = fixture.CreateDbContext();
        var user = await dbContext.Users.SingleAsync(candidate => candidate.Id == registered.UserId);
        Assert.Equal("new.user@example.com", user.Email.Value);
        Assert.NotEqual(password, user.PasswordHash.EncodedValue);
    }

    [Fact]
    public async Task Register_DuplicateEmailReturnsConflict()
    {
        await fixture.ResetAsync();
        var request = new { name = "Test User", email = "same@example.com", password = "password" };
        using var firstResponse = await fixture.Client.PostAsJsonAsync("/auth/register", request);
        using var duplicateResponse = await fixture.Client.PostAsJsonAsync(
            "/auth/register",
            new { request.name, email = "SAME@example.com", request.password });

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.Equal(
            "application/problem+json",
            duplicateResponse.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Register_InvalidRequestReturnsValidationProblem()
    {
        await fixture.ResetAsync();

        using var response = await fixture.Client.PostAsJsonAsync(
            "/auth/register",
            new { name = " ", email = "not-an-email", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(problem.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Login_ValidCredentialsIssueJwtAndExposeAuthenticatedIdentity()
    {
        await fixture.ResetAsync();
        var userId = await RegisterAndActivateAsync(
            "login@example.com",
            "correct horse battery staple",
            includeAdminRole: true);

        using var response = await fixture.Client.PostAsJsonAsync(
            "/auth/login",
            new { email = "LOGIN@example.com", password = "correct horse battery staple" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("correct horse battery staple", body, StringComparison.Ordinal);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        var login = JsonSerializer.Deserialize<LoginResponse>(body, JsonOptions);
        Assert.NotNull(login);
        Assert.Equal("Bearer", login.TokenType);
        Assert.True(login.ExpiresAtUtc > DateTimeOffset.UtcNow);

        var decodedToken = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken);
        Assert.Equal(userId.ToString("D"), decodedToken.Subject);
        Assert.Contains(decodedToken.Claims, claim => claim.Type == "role" && claim.Value == "User");
        Assert.Contains(decodedToken.Claims, claim => claim.Type == "role" && claim.Value == "Admin");
        Assert.Equal("SistemaInventarioVentas.Api.Tests", decodedToken.Issuer);
        Assert.Contains("SistemaInventarioVentas.Api.Tests.Client", decodedToken.Audiences);
        Assert.True(decodedToken.ValidTo > DateTime.UtcNow);

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        using var meResponse = await fixture.Client.SendAsync(meRequest);

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var currentUser = await meResponse.Content.ReadFromJsonAsync<CurrentUserResponse>(JsonOptions);
        Assert.NotNull(currentUser);
        Assert.Equal(userId, currentUser.UserId);
        Assert.Contains("User", currentUser.Roles);
        Assert.Contains("Admin", currentUser.Roles);
    }

    [Fact]
    public async Task Login_InvalidCredentialsDoNotRevealWhetherEmailExists()
    {
        await fixture.ResetAsync();
        await RegisterAndActivateAsync("existing@example.com", "correct password");

        using var existingUserResponse = await fixture.Client.PostAsJsonAsync(
            "/auth/login",
            new { email = "existing@example.com", password = "wrong password" });
        using var missingUserResponse = await fixture.Client.PostAsJsonAsync(
            "/auth/login",
            new { email = "missing@example.com", password = "wrong password" });

        Assert.Equal(HttpStatusCode.Unauthorized, existingUserResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, missingUserResponse.StatusCode);
        using var existingProblem = JsonDocument.Parse(await existingUserResponse.Content.ReadAsStringAsync());
        using var missingProblem = JsonDocument.Parse(await missingUserResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            existingProblem.RootElement.GetProperty("title").GetString(),
            missingProblem.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            existingProblem.RootElement.GetProperty("detail").GetString(),
            missingProblem.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Login_InactiveUserDoesNotReceiveTokenAndHasGenericFailure()
    {
        await fixture.ResetAsync();
        using var registrationResponse = await fixture.Client.PostAsJsonAsync(
            "/auth/register",
            new { name = "Inactive", email = "inactive@example.com", password = "correct password" });
        Assert.Equal(HttpStatusCode.Created, registrationResponse.StatusCode);

        using var response = await fixture.Client.PostAsJsonAsync(
            "/auth/login",
            new { email = "inactive@example.com", password = "correct password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("accessToken", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("inactive@example.com", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProtectedCurrentUser_RejectsMissingMalformedInvalidAndExpiredTokens()
    {
        await fixture.ResetAsync();
        using var anonymousResponse = await fixture.Client.GetAsync("/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        var otherSigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var now = DateTime.UtcNow;
        var rejectedTokens = new[]
        {
            "not-a-jwt",
            CreateToken(
                otherSigningKey,
                "SistemaInventarioVentas.Api.Tests",
                "SistemaInventarioVentas.Api.Tests.Client",
                now.AddMinutes(-1),
                now.AddMinutes(5)),
            CreateToken(
                fixture.JwtSigningKey,
                "wrong-issuer",
                "SistemaInventarioVentas.Api.Tests.Client",
                now.AddMinutes(-1),
                now.AddMinutes(5)),
            CreateToken(
                fixture.JwtSigningKey,
                "SistemaInventarioVentas.Api.Tests",
                "wrong-audience",
                now.AddMinutes(-1),
                now.AddMinutes(5)),
            CreateToken(
                fixture.JwtSigningKey,
                "SistemaInventarioVentas.Api.Tests",
                "SistemaInventarioVentas.Api.Tests.Client",
                now.AddMinutes(-10),
                now.AddMinutes(-5))
        };

        foreach (var token in rejectedTokens)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await fixture.Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    private async Task<Guid> RegisterAndActivateAsync(
        string email,
        string password,
        bool includeAdminRole = false)
    {
        using var response = await fixture.Client.PostAsJsonAsync(
            "/auth/register",
            new { name = "Test User", email, password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var registered = await response.Content.ReadFromJsonAsync<RegisterResponse>(JsonOptions);
        Assert.NotNull(registered);

        await using var dbContext = fixture.CreateDbContext();
        var user = await dbContext.Users.SingleAsync(candidate => candidate.Id == registered.UserId);
        user.Activate();
        if (includeAdminRole)
        {
            user.AssignRole(UserRole.Admin);
        }

        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    private static string CreateToken(
        string signingKey,
        string issuer,
        string audience,
        DateTime notBefore,
        DateTime expires)
    {
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims:
            [
                new Claim("sub", Guid.NewGuid().ToString("D")),
                new Claim("role", UserRole.User.ToString())
            ],
            notBefore: notBefore,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed record RegisterResponse(Guid UserId, bool IsActive);

    private sealed record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAtUtc);

    private sealed record CurrentUserResponse(Guid UserId, string[] Roles);
}
