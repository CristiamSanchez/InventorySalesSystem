using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.API.Tests;

public sealed class InventoryConcurrencyApiTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ConcurrentWithdrawals_AtMostOneSucceeds_AndOtherReturns409Conflict()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("API-CONCUR-WITHDRAW", "Concurrency Widget");

        // Set initial stock to 10
        var entryResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/entries",
            new { quantity = 10, reference = "Initial stock 10" });
        entryResponse.EnsureSuccessStatusCode();

        // Prepare two concurrent clients with Admin authentication
        var client1 = fixture.CreateClient();
        client1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("Admin"));

        var client2 = fixture.CreateClient();
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("Admin"));

        var barrier = new Barrier(2);

        var task1 = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await client1.PostAsJsonAsync(
                $"/api/inventory/{product.Identifier}/exits",
                new { quantity = 7, reference = "Withdrawal 1" });
        });

        var task2 = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await client2.PostAsJsonAsync(
                $"/api/inventory/{product.Identifier}/exits",
                new { quantity = 7, reference = "Withdrawal 2" });
        });

        var responses = await Task.WhenAll(task1, task2);
        var okResponse = responses.SingleOrDefault(r => r.StatusCode == HttpStatusCode.OK);
        var conflictResponse = responses.SingleOrDefault(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.NotNull(okResponse);
        Assert.NotNull(conflictResponse);

        // Verify ProblemDetails on the conflict response
        var problem = await conflictResponse.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.True(
            problem.Title == "Concurrency conflict" || problem.Title == "Insufficient stock",
            $"Expected conflict title but got '{problem.Title}'");

        // Verify final inventory stock via GET
        var getResponse = await fixture.Client.GetAsync($"/api/inventory/{product.Identifier}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var inventory = await getResponse.Content.ReadFromJsonAsync<InventoryDto>(JsonOptions);
        Assert.NotNull(inventory);
        Assert.Equal(3, inventory.Quantity);

        // Verify movements via GET
        var movementsResponse = await fixture.Client.GetAsync($"/api/inventory/{product.Identifier}/movements");
        Assert.Equal(HttpStatusCode.OK, movementsResponse.StatusCode);
        var movements = await movementsResponse.Content.ReadFromJsonAsync<List<MovementDto>>(JsonOptions);
        Assert.NotNull(movements);
        Assert.Equal(2, movements.Count); // Initial 10 + single exit of 7
    }

    [Fact]
    public async Task ConcurrentInitialization_ViaApi_Returns409ConflictForDuplicate()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("API-CONCUR-INIT", "Init Widget");

        var client1 = fixture.CreateClient();
        client1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("Admin"));

        var client2 = fixture.CreateClient();
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("Admin"));

        var barrier = new Barrier(2);

        var task1 = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await client1.PostAsJsonAsync(
                $"/api/inventory/{product.Identifier}/entries",
                new { quantity = 25, reference = "Init client 1" });
        });

        var task2 = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await client2.PostAsJsonAsync(
                $"/api/inventory/{product.Identifier}/entries",
                new { quantity = 25, reference = "Init client 2" });
        });

        var responses = await Task.WhenAll(task1, task2);
        var okResponse = responses.SingleOrDefault(r => r.StatusCode == HttpStatusCode.OK);
        var conflictResponse = responses.SingleOrDefault(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.NotNull(okResponse);
        Assert.NotNull(conflictResponse);

        var problem = await conflictResponse.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Equal("Concurrency conflict", problem.Title);

        // Verify single inventory record in database
        var getResponse = await fixture.Client.GetAsync($"/api/inventory/{product.Identifier}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var inventory = await getResponse.Content.ReadFromJsonAsync<InventoryDto>(JsonOptions);
        Assert.NotNull(inventory);
        Assert.Equal(25, inventory.Quantity);

        var movementsResponse = await fixture.Client.GetAsync($"/api/inventory/{product.Identifier}/movements");
        var movements = await movementsResponse.Content.ReadFromJsonAsync<List<MovementDto>>(JsonOptions);
        Assert.NotNull(movements);
        Assert.Single(movements);
    }

    private string CreateToken(string role)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: "SistemaInventarioVentas.Api.Tests",
            audience: "SistemaInventarioVentas.Api.Tests.Client",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString("D")),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("D")),
                new Claim("role", role)
            ],
            notBefore: now.AddMinutes(-1),
            expires: now.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(fixture.JwtSigningKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<ProductDto> CreateCatalogProductAsync(string identifier, string name)
    {
        var categoryResponse = await fixture.Client.PostAsJsonAsync(
            "/api/categories",
            new { name = $"Category for {identifier}" });
        categoryResponse.EnsureSuccessStatusCode();
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);

        var productResponse = await fixture.Client.PostAsJsonAsync(
            "/api/products",
            new { identifier, name, categoryId = category!.Id });
        productResponse.EnsureSuccessStatusCode();
        return (await productResponse.Content.ReadFromJsonAsync<ProductDto>(JsonOptions))!;
    }

    private sealed record CategoryDto(Guid Id, string Name, bool IsActive);
    private sealed record ProductDto(string Identifier, string Name, Guid CategoryId, string CategoryName, bool IsActive);
    private sealed record InventoryDto(string ProductIdentifier, int Quantity);
    private sealed record MovementDto(Guid Id, string ProductIdentifier, string MovementType, int Quantity, string Reference, DateTimeOffset OccurredAtUtc);
}
