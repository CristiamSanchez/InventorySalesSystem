using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.API.Tests;

public sealed class InventoryApiTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetInventory_WhenProductExistsWithInventory_ReturnsOkAndStock()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("PROD-101", "Widget A");

        using var entryResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/entries",
            new { quantity = 50, reference = "Initial PO-1001" });
        Assert.Equal(HttpStatusCode.OK, entryResponse.StatusCode);

        // Switch to ordinary User to verify read access without wiping database
        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("User"));

        using var getResponse = await fixture.Client.GetAsync($"/api/inventory/{product.Identifier}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var inventory = await getResponse.Content.ReadFromJsonAsync<InventoryDto>(JsonOptions);
        Assert.NotNull(inventory);
        Assert.Equal(product.Identifier, inventory.ProductIdentifier);
        Assert.Equal(50, inventory.Quantity);
    }

    [Fact]
    public async Task GetMovements_WhenInventoryExists_ReturnsChronologicalMovements()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("PROD-102", "Widget B");

        await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/entries",
            new { quantity = 100, reference = "Initial Receipt" });

        await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/exits",
            new { quantity = 30, reference = "Sales Order 1" });

        await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/adjustments",
            new { quantityDelta = -10, reason = "Transit damage" });

        // Switch to ordinary User to read movements
        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("User"));

        using var movementsResponse = await fixture.Client.GetAsync(
            $"/api/inventory/{product.Identifier}/movements");
        Assert.Equal(HttpStatusCode.OK, movementsResponse.StatusCode);

        var movements = await movementsResponse.Content.ReadFromJsonAsync<MovementDto[]>(JsonOptions);
        Assert.NotNull(movements);
        Assert.Equal(3, movements.Length);

        Assert.Equal("Entry", movements[0].MovementType);
        Assert.Equal(100, movements[0].Quantity);
        Assert.Equal("Initial Receipt", movements[0].Reference);

        Assert.Equal("Exit", movements[1].MovementType);
        Assert.Equal(30, movements[1].Quantity);
        Assert.Equal("Sales Order 1", movements[1].Reference);

        Assert.Equal("Adjustment", movements[2].MovementType);
        Assert.Equal(-10, movements[2].Quantity);
        Assert.Equal("Transit damage", movements[2].Reference);
    }

    [Fact]
    public async Task RecordEntry_ViaRouteAndBody_IncreasesStockAndCreatesMovement()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("PROD-103", "Widget C");

        // Route-based entry
        using var routeResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/entries",
            new { quantity = 40, reference = "Delivery A" });
        Assert.Equal(HttpStatusCode.OK, routeResponse.StatusCode);
        var routeResult = await routeResponse.Content.ReadFromJsonAsync<InventoryDto>(JsonOptions);
        Assert.NotNull(routeResult);
        Assert.Equal(40, routeResult.Quantity);

        // Body-based entry
        using var bodyResponse = await fixture.Client.PostAsJsonAsync(
            "/api/inventory/entries",
            new { productIdentifier = product.Identifier, quantity = 25, reference = "Delivery B" });
        Assert.Equal(HttpStatusCode.OK, bodyResponse.StatusCode);
        var bodyResult = await bodyResponse.Content.ReadFromJsonAsync<InventoryDto>(JsonOptions);
        Assert.NotNull(bodyResult);
        Assert.Equal(65, bodyResult.Quantity);

        // Verify movement count
        using var movementsResponse = await fixture.Client.GetAsync(
            $"/api/inventory/{product.Identifier}/movements");
        var movements = await movementsResponse.Content.ReadFromJsonAsync<MovementDto[]>(JsonOptions);
        Assert.NotNull(movements);
        Assert.Equal(2, movements.Length);
    }

    [Fact]
    public async Task RecordExit_ViaRouteAndBody_DecreasesStock()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("PROD-104", "Widget D");

        await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/entries",
            new { quantity = 50, reference = "Opening Stock" });

        // Route-based exit
        using var routeResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/exits",
            new { quantity = 15, reference = "Dispatch 1" });
        Assert.Equal(HttpStatusCode.OK, routeResponse.StatusCode);
        var routeResult = await routeResponse.Content.ReadFromJsonAsync<InventoryDto>(JsonOptions);
        Assert.Equal(35, routeResult!.Quantity);

        // Body-based exit
        using var bodyResponse = await fixture.Client.PostAsJsonAsync(
            "/api/inventory/exits",
            new { productIdentifier = product.Identifier, quantity = 10, reference = "Dispatch 2" });
        Assert.Equal(HttpStatusCode.OK, bodyResponse.StatusCode);
        var bodyResult = await bodyResponse.Content.ReadFromJsonAsync<InventoryDto>(JsonOptions);
        Assert.Equal(25, bodyResult!.Quantity);
    }

    [Fact]
    public async Task AdjustStock_PositiveAndNegativeAndAlias_UpdatesStockAccordingly()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("PROD-105", "Widget E");

        await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/entries",
            new { quantity = 50, reference = "Opening Stock" });

        // Positive adjustment via /adjustments route
        using var posResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/adjustments",
            new { quantityDelta = 15, reason = "Audit found extra" });
        Assert.Equal(HttpStatusCode.OK, posResponse.StatusCode);
        var posResult = await posResponse.Content.ReadFromJsonAsync<InventoryDto>(JsonOptions);
        Assert.Equal(65, posResult!.Quantity);

        // Negative adjustment via body route
        using var negResponse = await fixture.Client.PostAsJsonAsync(
            "/api/inventory/adjustments",
            new { productIdentifier = product.Identifier, quantityDelta = -20, reason = "Damaged pallet" });
        Assert.Equal(HttpStatusCode.OK, negResponse.StatusCode);
        var negResult = await negResponse.Content.ReadFromJsonAsync<InventoryDto>(JsonOptions);
        Assert.Equal(45, negResult!.Quantity);

        // Negative adjustment via /adjust alias route
        using var aliasResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/adjust",
            new { quantityDelta = -5, reason = "Sample deduction" });
        Assert.Equal(HttpStatusCode.OK, aliasResponse.StatusCode);
        var aliasResult = await aliasResponse.Content.ReadFromJsonAsync<InventoryDto>(JsonOptions);
        Assert.Equal(40, aliasResult!.Quantity);
    }

    [Fact]
    public async Task RecordExit_WhenStockInsufficient_ReturnsConflictAndPreservesIntegrity()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("PROD-106", "Widget F");

        await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/entries",
            new { quantity = 20, reference = "Opening Stock" });

        // Attempt to exit 30 when only 20 is in stock
        using var exitResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/exits",
            new { quantity = 30, reference = "Excessive Exit" });

        Assert.Equal(HttpStatusCode.Conflict, exitResponse.StatusCode);
        Assert.Equal("application/problem+json", exitResponse.Content.Headers.ContentType!.MediaType);

        using var problem = JsonDocument.Parse(await exitResponse.Content.ReadAsStringAsync());
        Assert.Equal(409, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Insufficient stock", problem.RootElement.GetProperty("title").GetString());

        // Verify inventory integrity: stock must remain 20 and movement history must remain 1
        using var checkResponse = await fixture.Client.GetAsync($"/api/inventory/{product.Identifier}");
        var inventory = await checkResponse.Content.ReadFromJsonAsync<InventoryDto>(JsonOptions);
        Assert.Equal(20, inventory!.Quantity);

        using var movementsResponse = await fixture.Client.GetAsync($"/api/inventory/{product.Identifier}/movements");
        var movements = await movementsResponse.Content.ReadFromJsonAsync<MovementDto[]>(JsonOptions);
        Assert.Single(movements!);
    }

    [Fact]
    public async Task AdjustStock_WhenAdjustmentResultsInNegativeStock_ReturnsConflictAndPreservesIntegrity()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("PROD-107", "Widget G");

        await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/entries",
            new { quantity = 15, reference = "Initial Stock" });

        using var adjustResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/adjustments",
            new { quantityDelta = -25, reason = "Oversized downward correction" });

        Assert.Equal(HttpStatusCode.Conflict, adjustResponse.StatusCode);
        Assert.Equal("application/problem+json", adjustResponse.Content.Headers.ContentType!.MediaType);

        // Verify stock remains untouched
        using var checkResponse = await fixture.Client.GetAsync($"/api/inventory/{product.Identifier}");
        var inventory = await checkResponse.Content.ReadFromJsonAsync<InventoryDto>(JsonOptions);
        Assert.Equal(15, inventory!.Quantity);

        using var movementsResponse = await fixture.Client.GetAsync($"/api/inventory/{product.Identifier}/movements");
        var movements = await movementsResponse.Content.ReadFromJsonAsync<MovementDto[]>(JsonOptions);
        Assert.Single(movements!);
    }

    [Fact]
    public async Task GetInventoryAndMovements_WhenNotFound_ReturnsNotFound()
    {
        await fixture.ResetAsAsync(UserRole.User);

        using var inventoryResponse = await fixture.Client.GetAsync("/api/inventory/NON-EXISTENT");
        Assert.Equal(HttpStatusCode.NotFound, inventoryResponse.StatusCode);

        using var movementsResponse = await fixture.Client.GetAsync("/api/inventory/NON-EXISTENT/movements");
        Assert.Equal(HttpStatusCode.NotFound, movementsResponse.StatusCode);
    }

    [Fact]
    public async Task RecordExitAndAdjustment_WhenInventoryNotFound_ReturnsNotFound()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("PROD-108", "Widget H");

        using var exitResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/exits",
            new { quantity = 5, reference = "Exit missing stock" });
        Assert.Equal(HttpStatusCode.NotFound, exitResponse.StatusCode);

        using var adjustResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/adjustments",
            new { quantityDelta = -5, reason = "Adjust missing stock" });
        Assert.Equal(HttpStatusCode.NotFound, adjustResponse.StatusCode);
    }

    [Fact]
    public async Task RecordEntry_WhenProductNotInCatalog_ReturnsNotFound()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var response = await fixture.Client.PostAsJsonAsync(
            "/api/inventory/NOT-IN-CATALOG/entries",
            new { quantity = 10, reference = "Receipt for ghost product" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, problem.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task InvalidPayloads_ReturnBadRequestOrValidationProblem()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("PROD-109", "Widget I");

        // Missing reference on entry
        using var missingRefResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/entries",
            new { quantity = 10 });
        Assert.Equal(HttpStatusCode.BadRequest, missingRefResponse.StatusCode);

        // Non-positive quantity on entry
        using var nonPosEntry = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/entries",
            new { quantity = 0, reference = "Zero receipt" });
        Assert.Equal(HttpStatusCode.BadRequest, nonPosEntry.StatusCode);

        // Missing reference on exit
        using var missingExitRef = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/exits",
            new { quantity = 10 });
        Assert.Equal(HttpStatusCode.BadRequest, missingExitRef.StatusCode);

        // Non-positive quantity on exit
        using var nonPosExit = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/exits",
            new { quantity = -5, reference = "Negative exit" });
        Assert.Equal(HttpStatusCode.BadRequest, nonPosExit.StatusCode);

        // Zero delta on adjustment
        using var zeroDelta = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/adjustments",
            new { quantityDelta = 0, reason = "Zero adjustment" });
        Assert.Equal(HttpStatusCode.BadRequest, zeroDelta.StatusCode);

        // Missing reason on adjustment
        using var missingReason = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/adjustments",
            new { quantityDelta = 5 });
        Assert.Equal(HttpStatusCode.BadRequest, missingReason.StatusCode);

        // Missing productIdentifier on body route
        using var missingProductBody = await fixture.Client.PostAsJsonAsync(
            "/api/inventory/entries",
            new { quantity = 10, reference = "Missing Product" });
        Assert.Equal(HttpStatusCode.BadRequest, missingProductBody.StatusCode);
    }

    [Fact]
    public async Task AnonymousRequests_ReturnUnauthorized()
    {
        await fixture.ResetAsync();

        using var getResponse = await fixture.Client.GetAsync("/api/inventory/PROD-101");
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);

        using var movementsResponse = await fixture.Client.GetAsync("/api/inventory/PROD-101/movements");
        Assert.Equal(HttpStatusCode.Unauthorized, movementsResponse.StatusCode);

        using var entryResponse = await fixture.Client.PostAsJsonAsync(
            "/api/inventory/PROD-101/entries",
            new { quantity = 10, reference = "Anon" });
        Assert.Equal(HttpStatusCode.Unauthorized, entryResponse.StatusCode);

        using var exitResponse = await fixture.Client.PostAsJsonAsync(
            "/api/inventory/PROD-101/exits",
            new { quantity = 10, reference = "Anon" });
        Assert.Equal(HttpStatusCode.Unauthorized, exitResponse.StatusCode);

        using var adjustResponse = await fixture.Client.PostAsJsonAsync(
            "/api/inventory/PROD-101/adjustments",
            new { quantityDelta = 5, reason = "Anon" });
        Assert.Equal(HttpStatusCode.Unauthorized, adjustResponse.StatusCode);
    }

    [Fact]
    public async Task UserRole_CannotMutateInventory_ReturnsForbidden()
    {
        // First create product and inventory as Admin
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("PROD-110", "Widget J");
        await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/entries",
            new { quantity = 50, reference = "Initial Stock" });

        // Switch to ordinary User without wiping database
        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("User"));

        // User can read inventory
        using var readResponse = await fixture.Client.GetAsync($"/api/inventory/{product.Identifier}");
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);

        // User cannot record entries
        using var entryResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/entries",
            new { quantity = 10, reference = "Forbidden entry" });
        Assert.Equal(HttpStatusCode.Forbidden, entryResponse.StatusCode);

        // User cannot record exits
        using var exitResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/exits",
            new { quantity = 10, reference = "Forbidden exit" });
        Assert.Equal(HttpStatusCode.Forbidden, exitResponse.StatusCode);

        // User cannot record adjustments
        using var adjustResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/adjustments",
            new { quantityDelta = 5, reason = "Forbidden adjustment" });
        Assert.Equal(HttpStatusCode.Forbidden, adjustResponse.StatusCode);
    }

    [Fact]
    public async Task RecordEntry_ViaRoute_WithSupplier_AssociatesSupplierAndReturnsSupplierDetailsInMovements()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("PROD-API-SUP1", "Widget SUP1");
        var supplier = await CreateSupplierAsync("Acme Global Distributors");

        using var entryResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/entries",
            new
            {
                quantity = 35,
                reference = "PO-SUP-ROUTE",
                supplierId = supplier.Id
            });
        Assert.Equal(HttpStatusCode.OK, entryResponse.StatusCode);

        using var movementsResponse = await fixture.Client.GetAsync(
            $"/api/inventory/{product.Identifier}/movements");
        Assert.Equal(HttpStatusCode.OK, movementsResponse.StatusCode);

        var movements = await movementsResponse.Content.ReadFromJsonAsync<MovementDto[]>(JsonOptions);
        Assert.NotNull(movements);
        var movement = Assert.Single(movements);
        Assert.Equal(supplier.Id, movement.SupplierId);
        Assert.Equal("Acme Global Distributors", movement.SupplierName);
        Assert.Equal(35, movement.Quantity);
        Assert.Equal("PO-SUP-ROUTE", movement.Reference);
    }

    [Fact]
    public async Task RecordEntry_ViaBody_WithSupplier_AssociatesSupplierAndReturnsSupplierDetailsInMovements()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("PROD-API-SUP2", "Widget SUP2");
        var supplier = await CreateSupplierAsync("Beta Parts Corp");

        using var entryResponse = await fixture.Client.PostAsJsonAsync(
            "/api/inventory/entries",
            new
            {
                productIdentifier = product.Identifier,
                quantity = 40,
                reference = "PO-SUP-BODY",
                supplierId = supplier.Id
            });
        Assert.Equal(HttpStatusCode.OK, entryResponse.StatusCode);

        using var movementsResponse = await fixture.Client.GetAsync(
            $"/api/inventory/{product.Identifier}/movements");
        Assert.Equal(HttpStatusCode.OK, movementsResponse.StatusCode);

        var movements = await movementsResponse.Content.ReadFromJsonAsync<MovementDto[]>(JsonOptions);
        Assert.NotNull(movements);
        var movement = Assert.Single(movements);
        Assert.Equal(supplier.Id, movement.SupplierId);
        Assert.Equal("Beta Parts Corp", movement.SupplierName);
        Assert.Equal(40, movement.Quantity);
    }

    [Fact]
    public async Task RecordEntry_WithNonExistentSupplierId_ReturnsNotFound()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("PROD-API-SUP3", "Widget SUP3");

        using var entryResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/entries",
            new
            {
                quantity = 10,
                reference = "PO-FAIL-404",
                supplierId = Guid.NewGuid()
            });
        Assert.Equal(HttpStatusCode.NotFound, entryResponse.StatusCode);
    }

    [Fact]
    public async Task RecordEntry_WithInactiveSupplierId_ReturnsBadRequest()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("PROD-API-SUP4", "Widget SUP4");
        var supplier = await CreateSupplierAsync("Inactive Supplier Ltd");

        using var deactivateResponse = await fixture.Client.PostAsync(
            $"/api/suppliers/{supplier.Id}/deactivate",
            null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        using var entryResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/entries",
            new
            {
                quantity = 10,
                reference = "PO-FAIL-400",
                supplierId = supplier.Id
            });
        Assert.Equal(HttpStatusCode.BadRequest, entryResponse.StatusCode);
    }

    [Fact]
    public async Task RecordEntry_WithoutSupplierId_SucceedsWithNullSupplierInMovements()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("PROD-API-SUP5", "Widget SUP5");

        using var entryResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{product.Identifier}/entries",
            new
            {
                quantity = 15,
                reference = "PO-WITHOUT-SUPPLIER"
            });
        Assert.Equal(HttpStatusCode.OK, entryResponse.StatusCode);

        using var movementsResponse = await fixture.Client.GetAsync(
            $"/api/inventory/{product.Identifier}/movements");
        Assert.Equal(HttpStatusCode.OK, movementsResponse.StatusCode);

        var movements = await movementsResponse.Content.ReadFromJsonAsync<MovementDto[]>(JsonOptions);
        Assert.NotNull(movements);
        var movement = Assert.Single(movements);
        Assert.Null(movement.SupplierId);
        Assert.Null(movement.SupplierName);
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

    private async Task<SupplierDto> CreateSupplierAsync(string name)
    {
        var response = await fixture.Client.PostAsJsonAsync(
            "/api/suppliers",
            new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SupplierDto>(JsonOptions))!;
    }

    private sealed record CategoryDto(Guid Id, string Name, bool IsActive);

    private sealed record ProductDto(string Identifier, string Name, Guid CategoryId, string CategoryName, bool IsActive);

    private sealed record SupplierDto(Guid Id, string Name, bool IsActive);

    private sealed record InventoryDto(string ProductIdentifier, int Quantity);

    private sealed record MovementDto(
        Guid Id,
        string ProductIdentifier,
        string MovementType,
        int Quantity,
        string Reference,
        DateTimeOffset OccurredAtUtc,
        Guid? SupplierId = null,
        string? SupplierName = null);
}
