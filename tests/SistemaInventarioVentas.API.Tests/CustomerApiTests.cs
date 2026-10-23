using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaInventarioVentas.API.Contracts;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.API.Tests;

public sealed class CustomerApiTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CreateCustomer_WithAllFields_ReturnsCreatedAndPersists()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var request = new CreateCustomerRequest
        {
            Name = "Global Logistics SA",
            IdentificationNumber = "RUC-20512345678",
            Email = "contact@globallogistics.com",
            Phone = "+51 987654321",
            Address = "Av. Central 123, Lima"
        };

        using var response = await fixture.Client.PostAsJsonAsync("/api/customers", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);
        Assert.NotNull(customer);
        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.Equal("Global Logistics SA", customer.Name);
        Assert.Equal("RUC-20512345678", customer.IdentificationNumber);
        Assert.Equal("contact@globallogistics.com", customer.Email);
        Assert.Equal("+51 987654321", customer.Phone);
        Assert.Equal("Av. Central 123, Lima", customer.Address);
        Assert.True(customer.IsActive);
        Assert.Contains(customer.Id.ToString(), response.Headers.Location.ToString());

        // Verify persistence directly in database
        await using var dbContext = fixture.CreateDbContext();
        var entity = await dbContext.Customers.SingleOrDefaultAsync(c => c.Id == customer.Id);
        Assert.NotNull(entity);
        Assert.Equal("Global Logistics SA", entity.Name);
        Assert.Equal("RUC-20512345678", entity.IdentificationNumber);
        Assert.Equal("contact@globallogistics.com", entity.Email?.Value);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public async Task CreateCustomer_WithMinimalFields_ReturnsCreated()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var request = new CreateCustomerRequest
        {
            Name = "Walk-in Counter Customer"
        };

        using var response = await fixture.Client.PostAsJsonAsync("/api/customers", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);
        Assert.NotNull(customer);
        Assert.Equal("Walk-in Counter Customer", customer.Name);
        Assert.Null(customer.IdentificationNumber);
        Assert.Null(customer.Email);
        Assert.Null(customer.Phone);
        Assert.Null(customer.Address);
        Assert.True(customer.IsActive);
    }

    [Fact]
    public async Task CreateCustomer_WhenDuplicateIdentification_ReturnsConflict409()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var first = new CreateCustomerRequest
        {
            Name = "First Company",
            IdentificationNumber = "TAX-998877"
        };
        using var firstResponse = await fixture.Client.PostAsJsonAsync("/api/customers", first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Attempt duplicate with lowercase variation
        var second = new CreateCustomerRequest
        {
            Name = "Second Company",
            IdentificationNumber = "tax-998877"
        };
        using var secondResponse = await fixture.Client.PostAsJsonAsync("/api/customers", second);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        var problem = await secondResponse.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Equal("Customer conflict", problem.Title);
    }

    [Theory]
    [InlineData("", "Customer name is required.")]
    [InlineData("   ", "Customer name cannot be empty.")]
    public async Task CreateCustomer_WithInvalidName_ReturnsBadRequest(string invalidName, string _)
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var request = new { name = invalidName };
        using var response = await fixture.Client.PostAsJsonAsync("/api/customers", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_WithInvalidEmail_ReturnsBadRequest()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var request = new CreateCustomerRequest
        {
            Name = "Customer Bad Email",
            Email = "not-a-valid-email"
        };
        using var response = await fixture.Client.PostAsJsonAsync("/api/customers", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCustomerById_WhenExists_ReturnsOkForUserRole()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var createRequest = new CreateCustomerRequest
        {
            Name = "Readable Customer",
            IdentificationNumber = "ID-READ-1"
        };
        using var createResponse = await fixture.Client.PostAsJsonAsync("/api/customers", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);
        Assert.NotNull(created);

        // Switch to ordinary User role to verify read access
        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("User"));

        using var getResponse = await fixture.Client.GetAsync($"/api/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var customer = await getResponse.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);
        Assert.NotNull(customer);
        Assert.Equal(created.Id, customer.Id);
        Assert.Equal("Readable Customer", customer.Name);
        Assert.Equal("ID-READ-1", customer.IdentificationNumber);
    }

    [Fact]
    public async Task GetCustomerById_WhenNotFound_ReturnsNotFound404()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var nonExistentId = Guid.NewGuid();

        using var response = await fixture.Client.GetAsync($"/api/customers/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Resource not found", problem.Title);
    }

    [Fact]
    public async Task ListCustomers_WithoutFilters_ReturnsAllCustomers()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        await fixture.Client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest { Name = "Customer 1" });
        await fixture.Client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest { Name = "Customer 2" });

        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("User"));

        using var response = await fixture.Client.GetAsync("/api/customers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<IReadOnlyList<CustomerResponse>>(JsonOptions);
        Assert.NotNull(list);
        Assert.True(list.Count >= 2);
    }

    [Fact]
    public async Task ListCustomers_FilterByActiveStatus_ReturnsFilteredList()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var create1 = await fixture.Client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest { Name = "Active Customer" });
        var customer1 = await create1.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);

        using var create2 = await fixture.Client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest { Name = "Deactivated Customer" });
        var customer2 = await create2.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);

        // Deactivate customer 2
        using var deact = await fixture.Client.PostAsync($"/api/customers/{customer2!.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deact.StatusCode);

        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("User"));

        // Query active only
        using var activeResponse = await fixture.Client.GetAsync("/api/customers?isActive=true");
        var activeList = await activeResponse.Content.ReadFromJsonAsync<IReadOnlyList<CustomerResponse>>(JsonOptions);
        Assert.NotNull(activeList);
        Assert.Contains(activeList, c => c.Id == customer1!.Id);
        Assert.DoesNotContain(activeList, c => c.Id == customer2.Id);

        // Query inactive only
        using var inactiveResponse = await fixture.Client.GetAsync("/api/customers?isActive=false");
        var inactiveList = await inactiveResponse.Content.ReadFromJsonAsync<IReadOnlyList<CustomerResponse>>(JsonOptions);
        Assert.NotNull(inactiveList);
        Assert.Contains(inactiveList, c => c.Id == customer2.Id);
        Assert.DoesNotContain(inactiveList, c => c.Id == customer1!.Id);
    }

    [Fact]
    public async Task ListCustomers_SearchTerm_MatchesAcrossFields()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        await fixture.Client.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest
            {
                Name = "Acme Distribution Corp",
                IdentificationNumber = "ID-ACME-99",
                Email = "orders@acme-corp.com",
                Phone = "555-0199"
            });

        await fixture.Client.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest
            {
                Name = "Zenith Hardware Ltd",
                IdentificationNumber = "ID-ZENITH-88",
                Email = "supply@zenith.com",
                Phone = "555-0288"
            });

        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("User"));

        // Search by name substring
        using var resName = await fixture.Client.GetAsync("/api/customers?searchTerm=distribution");
        var listName = await resName.Content.ReadFromJsonAsync<IReadOnlyList<CustomerResponse>>(JsonOptions);
        Assert.Single(listName!);
        Assert.Equal("Acme Distribution Corp", listName![0].Name);

        // Search by ID substring
        using var resId = await fixture.Client.GetAsync("/api/customers?searchTerm=zenith-88");
        var listId = await resId.Content.ReadFromJsonAsync<IReadOnlyList<CustomerResponse>>(JsonOptions);
        Assert.Single(listId!);
        Assert.Equal("Zenith Hardware Ltd", listId![0].Name);

        // Search by email substring
        using var resEmail = await fixture.Client.GetAsync("/api/customers?searchTerm=acme-corp");
        var listEmail = await resEmail.Content.ReadFromJsonAsync<IReadOnlyList<CustomerResponse>>(JsonOptions);
        Assert.Single(listEmail!);

        // Search by phone substring
        using var resPhone = await fixture.Client.GetAsync("/api/customers?searchTerm=0288");
        var listPhone = await resPhone.Content.ReadFromJsonAsync<IReadOnlyList<CustomerResponse>>(JsonOptions);
        Assert.Single(listPhone!);
    }

    [Fact]
    public async Task UpdateCustomerDetails_WithValidData_ReturnsOkAndPersists()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest
            {
                Name = "Original Name",
                IdentificationNumber = "ORIG-111",
                Email = "orig@test.com",
                Phone = "111",
                Address = "Addr 1"
            });
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);
        Assert.NotNull(created);

        var updateRequest = new UpdateCustomerRequest
        {
            Name = "Updated Name SA",
            IdentificationNumber = "UPD-222",
            Email = "updated@test.com",
            Phone = "222-333",
            Address = "New Address 456"
        };

        using var updateResponse = await fixture.Client.PutAsJsonAsync($"/api/customers/{created.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Updated Name SA", updated.Name);
        Assert.Equal("UPD-222", updated.IdentificationNumber);
        Assert.Equal("updated@test.com", updated.Email);
        Assert.Equal("222-333", updated.Phone);
        Assert.Equal("New Address 456", updated.Address);

        // Verify direct DB persistence
        await using var dbContext = fixture.CreateDbContext();
        var entity = await dbContext.Customers.SingleAsync(c => c.Id == created.Id);
        Assert.Equal("Updated Name SA", entity.Name);
        Assert.Equal("UPD-222", entity.IdentificationNumber);
        Assert.Equal("updated@test.com", entity.Email?.Value);
    }

    [Fact]
    public async Task UpdateCustomerDetails_WhenPreservingSameIdentification_ReturnsOk()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest
            {
                Name = "Self Company",
                IdentificationNumber = "SAME-ID-1"
            });
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);

        var updateRequest = new UpdateCustomerRequest
        {
            Name = "Self Company Renamed",
            IdentificationNumber = "same-id-1" // case-variation of same customer
        };

        using var updateResponse = await fixture.Client.PutAsJsonAsync($"/api/customers/{created!.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);
        Assert.Equal("Self Company Renamed", updated!.Name);
        Assert.Equal("SAME-ID-1", updated.IdentificationNumber);
    }

    [Fact]
    public async Task UpdateCustomerDetails_WhenConflictingWithOtherCustomer_ReturnsConflict409()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        await fixture.Client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest
        {
            Name = "Customer One",
            IdentificationNumber = "TAKEN-ID"
        });

        using var createTwo = await fixture.Client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest
        {
            Name = "Customer Two",
            IdentificationNumber = "OTHER-ID"
        });
        var customerTwo = await createTwo.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);

        var updateRequest = new UpdateCustomerRequest
        {
            Name = "Customer Two Updated",
            IdentificationNumber = "taken-id"
        };

        using var updateResponse = await fixture.Client.PutAsJsonAsync($"/api/customers/{customerTwo!.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
        var problem = await updateResponse.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Equal("Customer conflict", problem.Title);
    }

    [Fact]
    public async Task UpdateCustomerDetails_WhenCustomerNotFound_ReturnsNotFound404()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var updateRequest = new UpdateCustomerRequest { Name = "Ghost Customer" };
        using var response = await fixture.Client.PutAsJsonAsync($"/api/customers/{Guid.NewGuid()}", updateRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ActivateAndDeactivateCustomer_TransitionsStatusAndPersists()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest { Name = "Lifecycle Customer" });
        var customer = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);
        Assert.True(customer!.IsActive);

        // Deactivate
        using var deactResponse = await fixture.Client.PostAsync($"/api/customers/{customer.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactResponse.StatusCode);
        var deactivated = await deactResponse.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);
        Assert.False(deactivated!.IsActive);

        // Verify in DB
        await using (var dbContext = fixture.CreateDbContext())
        {
            var dbCust = await dbContext.Customers.SingleAsync(c => c.Id == customer.Id);
            Assert.False(dbCust.IsActive);
        }

        // Activate
        using var actResponse = await fixture.Client.PostAsync($"/api/customers/{customer.Id}/activate", null);
        Assert.Equal(HttpStatusCode.OK, actResponse.StatusCode);
        var activated = await actResponse.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);
        Assert.True(activated!.IsActive);

        // Verify in DB
        await using (var dbContext = fixture.CreateDbContext())
        {
            var dbCust = await dbContext.Customers.SingleAsync(c => c.Id == customer.Id);
            Assert.True(dbCust.IsActive);
        }
    }

    [Fact]
    public async Task ActivateAndDeactivate_WhenCustomerNotFound_ReturnsNotFound404()
    {
        await fixture.ResetAsAsync(UserRole.Admin);
        var nonExistentId = Guid.NewGuid();

        using var actResponse = await fixture.Client.PostAsync($"/api/customers/{nonExistentId}/activate", null);
        Assert.Equal(HttpStatusCode.NotFound, actResponse.StatusCode);

        using var deactResponse = await fixture.Client.PostAsync($"/api/customers/{nonExistentId}/deactivate", null);
        Assert.Equal(HttpStatusCode.NotFound, deactResponse.StatusCode);
    }

    [Fact]
    public async Task CustomerEndpoints_WhenAnonymous_ReturnsUnauthorized401()
    {
        await fixture.ResetAsync();
        fixture.Client.DefaultRequestHeaders.Authorization = null;

        var dummyId = Guid.NewGuid();

        using var getList = await fixture.Client.GetAsync("/api/customers");
        Assert.Equal(HttpStatusCode.Unauthorized, getList.StatusCode);

        using var getSingle = await fixture.Client.GetAsync($"/api/customers/{dummyId}");
        Assert.Equal(HttpStatusCode.Unauthorized, getSingle.StatusCode);

        using var post = await fixture.Client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest { Name = "X" });
        Assert.Equal(HttpStatusCode.Unauthorized, post.StatusCode);

        using var put = await fixture.Client.PutAsJsonAsync($"/api/customers/{dummyId}", new UpdateCustomerRequest { Name = "X" });
        Assert.Equal(HttpStatusCode.Unauthorized, put.StatusCode);

        using var act = await fixture.Client.PostAsync($"/api/customers/{dummyId}/activate", null);
        Assert.Equal(HttpStatusCode.Unauthorized, act.StatusCode);

        using var deact = await fixture.Client.PostAsync($"/api/customers/{dummyId}/deactivate", null);
        Assert.Equal(HttpStatusCode.Unauthorized, deact.StatusCode);
    }

    [Fact]
    public async Task CustomerMutations_WhenRoleIsUser_ReturnsForbidden403()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest { Name = "Permission Customer" });
        var customer = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);

        // Switch to User role (non-admin)
        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("User"));

        using var post = await fixture.Client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest { Name = "New Cust" });
        Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);

        using var put = await fixture.Client.PutAsJsonAsync($"/api/customers/{customer!.Id}", new UpdateCustomerRequest { Name = "Updated" });
        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);

        using var act = await fixture.Client.PostAsync($"/api/customers/{customer.Id}/activate", null);
        Assert.Equal(HttpStatusCode.Forbidden, act.StatusCode);

        using var deact = await fixture.Client.PostAsync($"/api/customers/{customer.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.Forbidden, deact.StatusCode);
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
}
