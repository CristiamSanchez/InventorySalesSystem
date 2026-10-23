using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SistemaInventarioVentas.API.Contracts;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.API.Tests;

public sealed class SaleApiTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CreateDraftSale_WithValidData_ReturnsCreatedAndSaleResponse()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var response = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerReference = "ACME Corporation" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var sale = await response.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(sale);
        Assert.NotEqual(Guid.Empty, sale.Id);
        Assert.Equal("ACME Corporation", sale.CustomerReference);
        Assert.Equal("Draft", sale.Status);
        Assert.Equal(0m, sale.Subtotal);
        Assert.Equal(0m, sale.TaxAmount);
        Assert.Equal(0m, sale.Total);
        Assert.Empty(sale.Details);
        Assert.Null(sale.ConfirmedAtUtc);
        Assert.Contains(sale.Id.ToString(), response.Headers.Location.ToString());
    }

    [Fact]
    public async Task CreateDraftSale_WhenCustomerReferenceEmpty_ReturnsValidationProblem()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var response = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerReference = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey(nameof(CreateSaleRequest.CustomerReference)));
    }

    [Fact]
    public async Task GetSaleById_WhenSaleExists_ReturnsOkAndSaleResponse()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerReference = "Customer Beta" });
        var created = await createResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(created);

        // Switch to ordinary User to verify read access for authenticated non-admin
        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("User"));

        using var getResponse = await fixture.Client.GetAsync($"/api/sales/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var sale = await getResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(sale);
        Assert.Equal(created.Id, sale.Id);
        Assert.Equal("Customer Beta", sale.CustomerReference);
        Assert.Equal("Draft", sale.Status);
    }

    [Fact]
    public async Task GetSaleById_WhenSaleNotFound_ReturnsNotFound()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var nonExistentId = Guid.NewGuid();

        using var response = await fixture.Client.GetAsync($"/api/sales/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Resource not found", problem.Title);
    }

    [Fact]
    public async Task AddDetail_WhenSaleIsDraft_AddsDetailAndRecalculatesTotals()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("PROD-ADD-1", "Product Add Detail");

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerReference = "Customer Detail Add" });
        var sale = await createResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(sale);

        using var detailResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/sales/{sale.Id}/details",
            new
            {
                productIdentifier = product.Identifier,
                quantity = 3,
                unitPrice = 15.00m,
                taxRate = 0.16m
            });

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        var updatedSale = await detailResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(updatedSale);
        Assert.Single(updatedSale.Details);

        var detail = updatedSale.Details[0];
        Assert.Equal(product.Identifier, detail.ProductIdentifier);
        Assert.Equal(3, detail.Quantity);
        Assert.Equal(15.00m, detail.UnitPrice);
        Assert.Equal(0.16m, detail.TaxRate);
        Assert.Equal(45.00m, detail.Subtotal);
        Assert.Equal(7.20m, detail.TaxAmount);
        Assert.Equal(52.20m, detail.Total);

        Assert.Equal(45.00m, updatedSale.Subtotal);
        Assert.Equal(7.20m, updatedSale.TaxAmount);
        Assert.Equal(52.20m, updatedSale.Total);
    }

    [Fact]
    public async Task AddDetail_WhenProductNotFound_ReturnsNotFound()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerReference = "Customer Missing Prod" });
        var sale = await createResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(sale);

        using var detailResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/sales/{sale.Id}/details",
            new
            {
                productIdentifier = "NON-EXISTENT-PROD",
                quantity = 2,
                unitPrice = 10.00m,
                taxRate = 0m
            });

        Assert.Equal(HttpStatusCode.NotFound, detailResponse.StatusCode);
        var problem = await detailResponse.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("Resource not found", problem.Title);
        Assert.Contains("Product was not found", problem.Detail);
    }

    [Fact]
    public async Task AddDetail_WhenQuantityOrPriceInvalid_ReturnsValidationProblem()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerReference = "Customer Invalid Detail" });
        var sale = await createResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(sale);

        // Quantity 0
        using var zeroQtyResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/sales/{sale.Id}/details",
            new
            {
                productIdentifier = "PROD-1",
                quantity = 0,
                unitPrice = 10.00m,
                taxRate = 0m
            });
        Assert.Equal(HttpStatusCode.BadRequest, zeroQtyResponse.StatusCode);

        // Negative unit price
        using var negPriceResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/sales/{sale.Id}/details",
            new
            {
                productIdentifier = "PROD-1",
                quantity = 1,
                unitPrice = -5.00m,
                taxRate = 0m
            });
        Assert.Equal(HttpStatusCode.BadRequest, negPriceResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateDetail_WhenSaleIsDraft_UpdatesDetailAndRecalculatesTotals()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var product = await CreateCatalogProductAsync("PROD-UPD-1", "Product Update Detail");

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerReference = "Customer Update" });
        var sale = await createResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(sale);

        using var addResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/sales/{sale.Id}/details",
            new
            {
                productIdentifier = product.Identifier,
                quantity = 2,
                unitPrice = 10.00m,
                taxRate = 0m
            });
        var saleWithDetail = await addResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(saleWithDetail);
        var detailId = saleWithDetail.Details[0].Id;

        using var updateResponse = await fixture.Client.PutAsJsonAsync(
            $"/api/sales/{sale.Id}/details/{detailId}",
            new
            {
                quantity = 5,
                unitPrice = 20.00m,
                taxRate = 0.16m
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Single(updated.Details);

        var detail = updated.Details[0];
        Assert.Equal(5, detail.Quantity);
        Assert.Equal(20.00m, detail.UnitPrice);
        Assert.Equal(0.16m, detail.TaxRate);
        Assert.Equal(100.00m, detail.Subtotal);
        Assert.Equal(16.00m, detail.TaxAmount);
        Assert.Equal(116.00m, detail.Total);

        Assert.Equal(100.00m, updated.Subtotal);
        Assert.Equal(16.00m, updated.TaxAmount);
        Assert.Equal(116.00m, updated.Total);
    }

    [Fact]
    public async Task RemoveDetail_WhenSaleIsDraft_RemovesDetailAndRecalculatesTotals()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var prod1 = await CreateCatalogProductAsync("PROD-REM-1", "Product Remove 1");
        var prod2 = await CreateCatalogProductAsync("PROD-REM-2", "Product Remove 2");

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerReference = "Customer Remove" });
        var sale = await createResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(sale);

        await fixture.Client.PostAsJsonAsync(
            $"/api/sales/{sale.Id}/details",
            new { productIdentifier = prod1.Identifier, quantity = 2, unitPrice = 10m, taxRate = 0m });
        using var add2Response = await fixture.Client.PostAsJsonAsync(
            $"/api/sales/{sale.Id}/details",
            new { productIdentifier = prod2.Identifier, quantity = 1, unitPrice = 30m, taxRate = 0m });
        var saleWithTwoDetails = await add2Response.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(saleWithTwoDetails);
        Assert.Equal(2, saleWithTwoDetails.Details.Count);
        Assert.Equal(50.00m, saleWithTwoDetails.Total);

        var detailId1 = saleWithTwoDetails.Details.Single(d => d.ProductIdentifier == prod1.Identifier).Id;

        using var removeResponse = await fixture.Client.DeleteAsync(
            $"/api/sales/{sale.Id}/details/{detailId1}");

        Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);
        var afterRemoval = await removeResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(afterRemoval);
        Assert.Single(afterRemoval.Details);
        Assert.Equal(prod2.Identifier, afterRemoval.Details[0].ProductIdentifier);
        Assert.Equal(30.00m, afterRemoval.Total);
    }

    [Fact]
    public async Task RemoveDetail_WhenDetailNotFound_ReturnsNotFound()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerReference = "Customer Remove Missing" });
        var sale = await createResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(sale);

        using var removeResponse = await fixture.Client.DeleteAsync(
            $"/api/sales/{sale.Id}/details/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, removeResponse.StatusCode);
    }

    [Fact]
    public async Task DetailMutations_WhenSaleIsConfirmed_ReturnsBadRequest()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var prod = await CreateCatalogProductAsync("PROD-CONF-MUT", "Product Confirmed Mutation");

        // Seed stock for the product so confirmation can succeed
        await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{prod.Identifier}/entries",
            new { quantity = 20, reference = "Initial Stock" });

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerReference = "Customer Locked" });
        var sale = await createResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(sale);

        using var addResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/sales/{sale.Id}/details",
            new { productIdentifier = prod.Identifier, quantity = 5, unitPrice = 10m, taxRate = 0m });
        var withDetail = await addResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(withDetail);
        var detailId = withDetail.Details[0].Id;

        // Confirm sale
        using var confirmResponse = await fixture.Client.PostAsync(
            $"/api/sales/{sale.Id}/confirm",
            null);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        // 1. Try to add detail after confirmation -> 400 Bad Request
        using var tryAdd = await fixture.Client.PostAsJsonAsync(
            $"/api/sales/{sale.Id}/details",
            new { productIdentifier = prod.Identifier, quantity = 1, unitPrice = 10m, taxRate = 0m });
        Assert.Equal(HttpStatusCode.BadRequest, tryAdd.StatusCode);
        var addProblem = await tryAdd.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(addProblem);
        Assert.Contains("Cannot modify details", addProblem.Detail);

        // 2. Try to update detail after confirmation -> 400 Bad Request
        using var tryUpdate = await fixture.Client.PutAsJsonAsync(
            $"/api/sales/{sale.Id}/details/{detailId}",
            new { quantity = 6, unitPrice = 10m, taxRate = 0m });
        Assert.Equal(HttpStatusCode.BadRequest, tryUpdate.StatusCode);
        var updateProblem = await tryUpdate.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(updateProblem);
        Assert.Contains("Cannot modify details", updateProblem.Detail);

        // 3. Try to remove detail after confirmation -> 400 Bad Request
        using var tryRemove = await fixture.Client.DeleteAsync(
            $"/api/sales/{sale.Id}/details/{detailId}");
        Assert.Equal(HttpStatusCode.BadRequest, tryRemove.StatusCode);
        var removeProblem = await tryRemove.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(removeProblem);
        Assert.Contains("Cannot modify details", removeProblem.Detail);
    }

    [Fact]
    public async Task AuthenticationAndRoleRestrictions_EnforcesSecurityPolicies()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var prod = await CreateCatalogProductAsync("PROD-SEC-1", "Security Test Product");

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerReference = "Security Customer" });
        var sale = await createResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(sale);

        // 1. Anonymous requests -> 401 Unauthorized
        fixture.Client.DefaultRequestHeaders.Authorization = null;

        using var anonPost = await fixture.Client.PostAsJsonAsync("/api/sales", new { customerReference = "Anon" });
        Assert.Equal(HttpStatusCode.Unauthorized, anonPost.StatusCode);

        using var anonGet = await fixture.Client.GetAsync($"/api/sales/{sale.Id}");
        Assert.Equal(HttpStatusCode.Unauthorized, anonGet.StatusCode);

        using var anonDetail = await fixture.Client.PostAsJsonAsync($"/api/sales/{sale.Id}/details", new { productIdentifier = prod.Identifier, quantity = 1, unitPrice = 10m, taxRate = 0m });
        Assert.Equal(HttpStatusCode.Unauthorized, anonDetail.StatusCode);

        using var anonConfirm = await fixture.Client.PostAsync($"/api/sales/{sale.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.Unauthorized, anonConfirm.StatusCode);

        // 2. Authenticated non-admin (User role) -> 403 Forbidden for mutations, 200 OK for reads
        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("User"));

        using var userGet = await fixture.Client.GetAsync($"/api/sales/{sale.Id}");
        Assert.Equal(HttpStatusCode.OK, userGet.StatusCode);

        using var userCreate = await fixture.Client.PostAsJsonAsync("/api/sales", new { customerReference = "Forbidden" });
        Assert.Equal(HttpStatusCode.Forbidden, userCreate.StatusCode);

        using var userAddDetail = await fixture.Client.PostAsJsonAsync($"/api/sales/{sale.Id}/details", new { productIdentifier = prod.Identifier, quantity = 1, unitPrice = 10m, taxRate = 0m });
        Assert.Equal(HttpStatusCode.Forbidden, userAddDetail.StatusCode);

        using var userUpdateDetail = await fixture.Client.PutAsJsonAsync($"/api/sales/{sale.Id}/details/{Guid.NewGuid()}", new { quantity = 2 });
        Assert.Equal(HttpStatusCode.Forbidden, userUpdateDetail.StatusCode);

        using var userRemoveDetail = await fixture.Client.DeleteAsync($"/api/sales/{sale.Id}/details/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, userRemoveDetail.StatusCode);

        using var userConfirm = await fixture.Client.PostAsync($"/api/sales/{sale.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.Forbidden, userConfirm.StatusCode);
    }

    [Fact]
    public async Task ConfirmSale_WhenSufficientStock_AtomicallyConfirmsSaleDeductsStockAndCreatesExitMovement()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var prod = await CreateCatalogProductAsync("PROD-CONF-OK", "Product Confirm OK");

        // Seed stock of 25 units
        using var entryResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{prod.Identifier}/entries",
            new { quantity = 25, reference = "Initial Stock 25" });
        Assert.Equal(HttpStatusCode.OK, entryResponse.StatusCode);

        // Create draft sale with 10 units
        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerReference = "Customer Confirm Success" });
        var sale = await createResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(sale);

        await fixture.Client.PostAsJsonAsync(
            $"/api/sales/{sale.Id}/details",
            new { productIdentifier = prod.Identifier, quantity = 10, unitPrice = 12.50m, taxRate = 0m });

        // Confirm sale
        using var confirmResponse = await fixture.Client.PostAsync(
            $"/api/sales/{sale.Id}/confirm",
            null);

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var confirmedSale = await confirmResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(confirmedSale);
        Assert.Equal("Confirmed", confirmedSale.Status);
        Assert.NotNull(confirmedSale.ConfirmedAtUtc);

        // Verify inventory stock is now 15 (25 - 10)
        using var invResponse = await fixture.Client.GetAsync($"/api/inventory/{prod.Identifier}");
        Assert.Equal(HttpStatusCode.OK, invResponse.StatusCode);
        var inventory = await invResponse.Content.ReadFromJsonAsync<InventoryDto>(JsonOptions);
        Assert.NotNull(inventory);
        Assert.Equal(15, inventory.Quantity);

        // Verify inventory movements include Exit referencing sale
        using var movResponse = await fixture.Client.GetAsync($"/api/inventory/{prod.Identifier}/movements");
        Assert.Equal(HttpStatusCode.OK, movResponse.StatusCode);
        var movements = await movResponse.Content.ReadFromJsonAsync<IReadOnlyList<MovementDto>>(JsonOptions);
        Assert.NotNull(movements);
        Assert.Equal(2, movements.Count);

        var exitMovement = movements.Single(m => m.MovementType == "Exit");
        Assert.Equal(10, exitMovement.Quantity);
        Assert.Equal($"Sale {sale.Id}", exitMovement.Reference);
    }

    [Fact]
    public async Task ConfirmSale_WhenInsufficientStock_ReturnsConflictAndDoesNotChangeStockOrStatus()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var prod = await CreateCatalogProductAsync("PROD-CONF-SHORT", "Product Short Stock");

        // Seed stock of 5 units
        await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{prod.Identifier}/entries",
            new { quantity = 5, reference = "Initial Stock 5" });

        // Create draft sale requesting 12 units
        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerReference = "Customer Short Stock" });
        var sale = await createResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(sale);

        await fixture.Client.PostAsJsonAsync(
            $"/api/sales/{sale.Id}/details",
            new { productIdentifier = prod.Identifier, quantity = 12, unitPrice = 10m, taxRate = 0m });

        // Attempt confirmation
        using var confirmResponse = await fixture.Client.PostAsync(
            $"/api/sales/{sale.Id}/confirm",
            null);

        Assert.Equal(HttpStatusCode.Conflict, confirmResponse.StatusCode);
        var problem = await confirmResponse.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("Insufficient stock", problem.Title);
        Assert.Contains("Insufficient stock for product 'PROD-CONF-SHORT'", problem.Detail);

        // Verify sale is still Draft
        using var getSaleResponse = await fixture.Client.GetAsync($"/api/sales/{sale.Id}");
        var saleAfter = await getSaleResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(saleAfter);
        Assert.Equal("Draft", saleAfter.Status);
        Assert.Null(saleAfter.ConfirmedAtUtc);

        // Verify stock is untouched (5 units)
        using var invResponse = await fixture.Client.GetAsync($"/api/inventory/{prod.Identifier}");
        var inventory = await invResponse.Content.ReadFromJsonAsync<InventoryDto>(JsonOptions);
        Assert.NotNull(inventory);
        Assert.Equal(5, inventory.Quantity);

        // Verify no exit movements exist
        using var movResponse = await fixture.Client.GetAsync($"/api/inventory/{prod.Identifier}/movements");
        var movements = await movResponse.Content.ReadFromJsonAsync<IReadOnlyList<MovementDto>>(JsonOptions);
        Assert.NotNull(movements);
        Assert.DoesNotContain(movements, m => m.MovementType == "Exit");
    }

    [Fact]
    public async Task CreateDraftSale_WithRegisteredCustomerId_ReturnsCreatedAndSnapshotsCustomerName()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var customer = await CreateCustomerAsync("Enterprise Inc.");

        using var response = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerId = customer.Id });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var sale = await response.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(sale);
        Assert.Equal(customer.Id, sale.CustomerId);
        Assert.Equal("Enterprise Inc.", sale.CustomerReference);
        Assert.Equal("Draft", sale.Status);
    }

    [Fact]
    public async Task CreateDraftSale_WithNonexistentCustomerId_ReturnsNotFound()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var nonexistentCustomerId = Guid.NewGuid();

        using var response = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerId = nonexistentCustomerId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Customer was not found.", problem.Detail);
    }

    [Fact]
    public async Task CreateDraftSale_WithInactiveCustomerId_ReturnsBadRequest()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var customer = await CreateCustomerAsync("Inactive Corp");
        var deactivateResponse = await fixture.Client.PostAsync(
            $"/api/customers/{customer.Id}/deactivate",
            null);
        deactivateResponse.EnsureSuccessStatusCode();

        using var response = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerId = customer.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("Cannot create a sale for an inactive customer.", problem.Detail);
    }

    [Fact]
    public async Task UpdateSaleCustomer_OnDraftSale_AssignsCustomerAndSnapshotsName()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        // Create walk-in draft sale
        using var createSaleResponse = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerReference = "Walk-in Buyer" });
        createSaleResponse.EnsureSuccessStatusCode();
        var sale = await createSaleResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(sale);
        Assert.Null(sale.CustomerId);

        // Create registered customer
        var customer = await CreateCustomerAsync("Loyal Client LLC");

        // Update sale to link customer
        using var updateResponse = await fixture.Client.PutAsJsonAsync(
            $"/api/sales/{sale.Id}/customer",
            new { customerId = customer.Id });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(customer.Id, updated.CustomerId);
        Assert.Equal("Loyal Client LLC", updated.CustomerReference);
    }

    [Fact]
    public async Task UpdateSaleCustomer_OnDraftSale_ClearsCustomerAndUpdatesWalkInReference()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var customer = await CreateCustomerAsync("Temporary Registered");

        // Create sale with registered customer
        using var createSaleResponse = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerId = customer.Id });
        createSaleResponse.EnsureSuccessStatusCode();
        var sale = await createSaleResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(sale);
        Assert.Equal(customer.Id, sale.CustomerId);

        // Clear customer association back to walk-in
        using var updateResponse = await fixture.Client.PutAsJsonAsync(
            $"/api/sales/{sale.Id}/customer",
            new { customerId = (Guid?)null, customerReference = "Reverted Walk-in" });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Null(updated.CustomerId);
        Assert.Equal("Reverted Walk-in", updated.CustomerReference);
    }

    [Fact]
    public async Task UpdateSaleCustomer_WhenSaleConfirmed_ReturnsBadRequest()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var prod = await CreateCatalogProductAsync("PROD-CUST-LOCK", "Lock Test Product");
        await fixture.Client.PostAsJsonAsync(
            $"/api/inventory/{prod.Identifier}/entries",
            new { quantity = 10, reference = "Initial" });

        var customerA = await CreateCustomerAsync("Customer Before Confirmation");
        var customerB = await CreateCustomerAsync("Customer After Confirmation");

        // Create sale, add detail, confirm
        using var createSaleResponse = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerId = customerA.Id });
        var sale = await createSaleResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(sale);

        await fixture.Client.PostAsJsonAsync(
            $"/api/sales/{sale.Id}/details",
            new { productIdentifier = prod.Identifier, quantity = 1, unitPrice = 10.00m, taxRate = 0m });

        var confirmResponse = await fixture.Client.PostAsync($"/api/sales/{sale.Id}/confirm", null);
        confirmResponse.EnsureSuccessStatusCode();

        // Attempt to change customer after confirmation
        using var updateResponse = await fixture.Client.PutAsJsonAsync(
            $"/api/sales/{sale.Id}/customer",
            new { customerId = customerB.Id });

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
        var problem = await updateResponse.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Contains("Cannot modify customer of a sale in 'Confirmed' status.", problem.Detail);
    }

    [Fact]
    public async Task UpdateSaleCustomer_Authorization_EnforcesAdminOnlyAndRejectsUserWithForbidden()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var createSaleResponse = await fixture.Client.PostAsJsonAsync(
            "/api/sales",
            new { customerReference = "Admin Created" });
        var sale = await createSaleResponse.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(sale);

        // Switch to User role
        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("User"));

        using var forbiddenResponse = await fixture.Client.PutAsJsonAsync(
            $"/api/sales/{sale.Id}/customer",
            new { customerReference = "Unauthorized Edit" });

        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        // Switch to no auth
        fixture.Client.DefaultRequestHeaders.Authorization = null;

        using var unauthorizedResponse = await fixture.Client.PutAsJsonAsync(
            $"/api/sales/{sale.Id}/customer",
            new { customerReference = "Unauthorized Edit" });

        Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);
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

    private async Task<CustomerDto> CreateCustomerAsync(string name, string? idNumber = null)
    {
        var response = await fixture.Client.PostAsJsonAsync(
            "/api/customers",
            new { name, identificationNumber = idNumber });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerDto>(JsonOptions))!;
    }

    private sealed record CustomerDto(Guid Id, string Name, string? IdentificationNumber, bool IsActive);

    private sealed record CategoryDto(Guid Id, string Name, bool IsActive);

    private sealed record ProductDto(string Identifier, string Name, Guid CategoryId, string CategoryName, bool IsActive);

    private sealed record InventoryDto(string ProductIdentifier, int Quantity);

    private sealed record MovementDto(Guid Id, string ProductIdentifier, string MovementType, int Quantity, string Reference, DateTimeOffset OccurredAtUtc);
}
