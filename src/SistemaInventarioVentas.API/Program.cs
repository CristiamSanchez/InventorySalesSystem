using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using SistemaInventarioVentas.API.Authentication;
using SistemaInventarioVentas.API.Endpoints;
using SistemaInventarioVentas.API.ErrorHandling;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.UseCases;
using SistemaInventarioVentas.Infrastructure.Persistence;
using SistemaInventarioVentas.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddScoped<CategoryUseCases>();
builder.Services.AddScoped<ProductUseCases>();
builder.Services.AddScoped<UserAuthenticationUseCases>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

var connectionString = builder.Configuration.GetConnectionString("InventoryDatabase");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'InventoryDatabase' must be configured through application settings or environment.");
}

builder.Services.AddPersistence(connectionString);
builder.Services.AddOptions<JwtTokenOptions>()
    .BindConfiguration(JwtTokenOptions.SectionName)
    .Validate(
        options => options.IsValid,
        "JWT configuration requires an issuer, audience, a signing key of at least 32 UTF-8 bytes, and a token lifetime from 1 to 60 minutes.")
    .ValidateOnStart();
builder.Services.AddJwtAccessTokenIssuer();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtTokenOptions>>((options, tokenOptions) =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = tokenOptions.Value.CreateTokenValidationParameters();
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var subject = context.Principal?.FindFirst("sub")?.Value;
                if (!Guid.TryParse(subject, out _))
                {
                    context.Fail("The access token subject is invalid.");
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapAuthentication();
app.MapCategories();
app.MapProducts();

app.Run();

/// <summary>Exposes the entry point to the API integration-test host.</summary>
public partial class Program;
