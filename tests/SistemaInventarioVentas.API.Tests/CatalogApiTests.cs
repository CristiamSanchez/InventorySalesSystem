using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.API.Tests;

public sealed class CatalogApiTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Categories_CreateGetAndList_ReturnExpectedResources()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/categories",
            new { name = "Hardware" });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Hardware", created.Name);

        using var getResponse = await fixture.Client.GetAsync(createResponse.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var retrieved = await getResponse.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);
        Assert.Equal(created.Id, retrieved!.Id);

        using var listResponse = await fixture.Client.GetAsync("/api/categories");
        var categories = await listResponse.Content.ReadFromJsonAsync<CategoryDto[]>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Contains(categories!, category => category.Id == created.Id);
    }

    [Fact]
    public async Task Categories_UpdateAndRemove_WhenUnused_ReturnSuccess()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var category = await CreateCategoryAsync("Before");

        using var updateResponse = await fixture.Client.PutAsJsonAsync(
            $"/api/categories/{category.Id}",
            new { name = "After", isActive = false });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);
        Assert.Equal("After", updated!.Name);
        Assert.False(updated.IsActive);

        using var deleteResponse = await fixture.Client.DeleteAsync($"/api/categories/{category.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Categories_InvalidRequest_ReturnsValidationProblem()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var response = await fixture.Client.PostAsJsonAsync(
            "/api/categories",
            new { name = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(problem.RootElement.TryGetProperty("errors", out _));

        using var missingStatusResponse = await fixture.Client.PutAsJsonAsync(
            $"/api/categories/{Guid.NewGuid()}",
            new { name = "Category" });
        Assert.Equal(HttpStatusCode.BadRequest, missingStatusResponse.StatusCode);
    }

    [Fact]
    public async Task Categories_NotFoundAndDuplicate_ReturnExpectedProblemStatuses()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var missingResponse = await fixture.Client.GetAsync($"/api/categories/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);

        await CreateCategoryAsync("Duplicate");
        using var duplicateResponse = await fixture.Client.PostAsJsonAsync(
            "/api/categories",
            new { name = "Duplicate" });
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task Categories_RemoveWithActiveProduct_ReturnsConflict()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var category = await CreateCategoryAsync("Used");
        await CreateProductAsync("USED-1", "Used product", category.Id);

        using var response = await fixture.Client.DeleteAsync($"/api/categories/{category.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Categories_RemoveWithInactiveProduct_ReturnsDatabaseConflict()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var category = await CreateCategoryAsync("Inactive use");
        await CreateProductAsync("USED-2", "Product", category.Id);
        using var categoryUpdateResponse = await fixture.Client.PutAsJsonAsync(
            $"/api/categories/{category.Id}",
            new { name = category.Name, isActive = false });
        Assert.Equal(HttpStatusCode.OK, categoryUpdateResponse.StatusCode);
        using var productUpdateResponse = await fixture.Client.PutAsJsonAsync(
            "/api/products/USED-2",
            new { name = "Product", categoryId = category.Id, isActive = false });
        Assert.Equal(HttpStatusCode.OK, productUpdateResponse.StatusCode);

        using var deleteResponse = await fixture.Client.DeleteAsync($"/api/categories/{category.Id}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        Assert.Equal("application/problem+json", deleteResponse.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Products_CreateGetAndListWithFilters_ReturnExpectedResources()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var category = await CreateCategoryAsync("Hardware");

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/products",
            new { identifier = "SKU-001", name = "Keyboard", categoryId = category.Id });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Hardware", created.CategoryName);

        using var getResponse = await fixture.Client.GetAsync(createResponse.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var retrieved = await getResponse.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        Assert.Equal(created.Identifier, retrieved!.Identifier);

        using var listResponse = await fixture.Client.GetAsync(
            $"/api/products?categoryId={category.Id}&isActive=true&searchTerm=key");
        var products = await listResponse.Content.ReadFromJsonAsync<ProductDto[]>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal("SKU-001", Assert.Single(products!).Identifier);
    }

    [Fact]
    public async Task Products_Update_ReturnsUpdatedProduct()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var category = await CreateCategoryAsync("Hardware");
        await CreateProductAsync("SKU-002", "Mouse", category.Id);

        using var response = await fixture.Client.PutAsJsonAsync(
            "/api/products/SKU-002",
            new { name = "Wireless mouse", categoryId = category.Id, isActive = false });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        Assert.Equal("Wireless mouse", updated!.Name);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task Products_InvalidRequest_ReturnsBadRequest()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var category = await CreateCategoryAsync("Hardware");

        using var missingFieldResponse = await fixture.Client.PostAsJsonAsync(
            "/api/products",
            new { identifier = (string?)null, name = "Product", categoryId = category.Id });
        Assert.Equal(HttpStatusCode.BadRequest, missingFieldResponse.StatusCode);

        using var missingCategoryResponse = await fixture.Client.PostAsJsonAsync(
            "/api/products",
            new { identifier = "SKU-INVALID", name = "Product" });
        Assert.Equal(HttpStatusCode.BadRequest, missingCategoryResponse.StatusCode);

        using var invalidIdentifierResponse = await fixture.Client.GetAsync("/api/products/%20");
        Assert.Equal(HttpStatusCode.BadRequest, invalidIdentifierResponse.StatusCode);
    }

    [Fact]
    public async Task Products_NotFoundAndDuplicateIdentifier_ReturnExpectedStatuses()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var category = await CreateCategoryAsync("Hardware");

        using var missingResponse = await fixture.Client.GetAsync("/api/products/UNKNOWN");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);

        await CreateProductAsync("SKU-003", "Product", category.Id);
        using var duplicateResponse = await fixture.Client.PostAsJsonAsync(
            "/api/products",
            new { identifier = "SKU-003", name = "Another product", categoryId = category.Id });
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task Products_CannotBeCreatedInInactiveCategory()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var category = await CreateCategoryAsync("Inactive category");
        using var updateResponse = await fixture.Client.PutAsJsonAsync(
            $"/api/categories/{category.Id}",
            new { name = category.Name, isActive = false });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/products",
            new { identifier = "SKU-004", name = "Product", categoryId = category.Id });

        Assert.Equal(HttpStatusCode.Conflict, createResponse.StatusCode);
    }

    [Fact]
    public async Task DevelopmentOpenApiDocument_DescribesCatalogEndpoints()
    {
        using var response = await fixture.Client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/categories", document, StringComparison.Ordinal);
        Assert.Contains("/api/products", document, StringComparison.Ordinal);
        Assert.Contains("/auth/register", document, StringComparison.Ordinal);
        Assert.Contains("/auth/login", document, StringComparison.Ordinal);
        Assert.Contains("/auth/me", document, StringComparison.Ordinal);
    }

    private async Task<CategoryDto> CreateCategoryAsync(string name)
    {
        using var response = await fixture.Client.PostAsJsonAsync("/api/categories", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions))!;
    }

    private async Task<ProductDto> CreateProductAsync(string identifier, string name, Guid categoryId)
    {
        using var response = await fixture.Client.PostAsJsonAsync(
            "/api/products",
            new { identifier, name, categoryId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>(JsonOptions))!;
    }

    private sealed record CategoryDto(Guid Id, string Name, bool IsActive);

    private sealed record ProductDto(
        string Identifier,
        string Name,
        Guid CategoryId,
        string CategoryName,
        bool IsActive);
}
