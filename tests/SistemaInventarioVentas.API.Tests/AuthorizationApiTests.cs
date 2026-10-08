using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.API.Tests;

public sealed class AuthorizationApiTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task AnonymousCatalogRequests_ReturnUnauthorizedProblemDetails()
    {
        await fixture.ResetAsync();

        using var readResponse = await fixture.Client.GetAsync("/api/categories");
        using var writeResponse = await fixture.Client.PostAsJsonAsync(
            "/api/categories",
            new { name = "Anonymous category" });

        Assert.Equal(HttpStatusCode.Unauthorized, readResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, writeResponse.StatusCode);
        Assert.Equal("application/problem+json", readResponse.Content.Headers.ContentType!.MediaType);
        Assert.Equal("application/problem+json", writeResponse.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task User_CanReadCatalogButCannotCreateCatalogRecords()
    {
        await fixture.ResetAsAsync(UserRole.User);

        using var readResponse = await fixture.Client.GetAsync("/api/categories");
        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/categories",
            new { name = "User category" });

        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
        Assert.Equal("application/problem+json", createResponse.Content.Headers.ContentType!.MediaType);
        using var problem = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        Assert.Equal(403, problem.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Admin_CanCreateCatalogRecords()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var response = await fixture.Client.PostAsJsonAsync(
            "/api/categories",
            new { name = "Admin category" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task MultipleAssignedRoles_ArePreservedAndAdminRoleGrantsAdminOperation()
    {
        await fixture.ResetAsAsync(UserRole.Admin, includeUserRole: true);

        using var identityResponse = await fixture.Client.GetAsync("/auth/me");
        Assert.Equal(HttpStatusCode.OK, identityResponse.StatusCode);
        var currentUser = await identityResponse.Content.ReadFromJsonAsync<CurrentUserResponse>(JsonOptions);
        Assert.NotNull(currentUser);
        Assert.Contains("User", currentUser.Roles);
        Assert.Contains("Admin", currentUser.Roles);

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/categories",
            new { name = "Multi-role category" });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    }

    [Fact]
    public async Task UnrecognizedRoleClaim_DoesNotGrantAdminAccess()
    {
        await fixture.ResetAsync();
        var token = CreateRoleToken("Manager");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/categories")
        {
            Content = JsonContent.Create(new { name = "Unexpected role category" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RegistrationRoleField_CannotElevateNewUserToAdmin()
    {
        await fixture.ResetAsync();
        using var registrationResponse = await fixture.Client.PostAsJsonAsync(
            "/auth/register",
            new
            {
                name = "Regular user",
                email = "regular@example.com",
                password = "test-password",
                role = "Admin",
                roles = new[] { "Admin" }
            });
        Assert.Equal(HttpStatusCode.Created, registrationResponse.StatusCode);
        var registration = await registrationResponse.Content.ReadFromJsonAsync<RegisterResponse>(JsonOptions);
        Assert.NotNull(registration);

        await using (var dbContext = fixture.CreateDbContext())
        {
            var user = await dbContext.Users.SingleAsync(candidate => candidate.Id == registration.UserId);
            Assert.Equal(new[] { UserRole.User }, user.Roles);
            user.Activate();
            await dbContext.SaveChangesAsync();
        }

        using var loginResponse = await fixture.Client.PostAsJsonAsync(
            "/auth/login",
            new { email = "regular@example.com", password = "test-password" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.NotNull(login);

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/categories")
        {
            Content = JsonContent.Create(new { name = "Role escalation attempt" })
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        using var createResponse = await fixture.Client.SendAsync(createRequest);

        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
    }

    private string CreateRoleToken(string role)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: "SistemaInventarioVentas.Api.Tests",
            audience: "SistemaInventarioVentas.Api.Tests.Client",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString("D")),
                new Claim("role", role)
            ],
            notBefore: now.AddMinutes(-1),
            expires: now.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(fixture.JwtSigningKey)),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed record CurrentUserResponse(Guid UserId, string[] Roles);

    private sealed record RegisterResponse(Guid UserId, bool IsActive);

    private sealed record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAtUtc);
}
