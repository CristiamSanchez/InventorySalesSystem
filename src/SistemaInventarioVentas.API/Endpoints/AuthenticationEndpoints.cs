using Microsoft.AspNetCore.Http.HttpResults;
using SistemaInventarioVentas.API.Authentication;
using SistemaInventarioVentas.API.Contracts;
using SistemaInventarioVentas.API.ErrorHandling;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Application.UseCases;

namespace SistemaInventarioVentas.API.Endpoints;

internal static class AuthenticationEndpoints
{
    public static RouteGroupBuilder MapAuthentication(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth").WithTags("Authentication");

        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .WithName("RegisterUser")
            .WithSummary("Register a user account")
            .WithDescription("Creates an inactive user account. The password is hashed and is never returned.")
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .WithName("LoginUser")
            .WithSummary("Authenticate and issue a JWT bearer access token")
            .WithDescription("Returns a short-lived access token only when credentials are valid and the account is active.")
            .Produces<LoginResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/me", GetCurrentUser)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithSummary("Get the authenticated user's identity and roles")
            .WithDescription("Returns identity and role claims from a validated bearer access token.")
            .Produces<CurrentUserResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<Results<Created<RegisterResponse>, ValidationProblem, ProblemHttpResult>>
        RegisterAsync(
            RegisterRequest request,
            UserAuthenticationUseCases useCases,
            HttpContext httpContext,
            CancellationToken cancellationToken)
    {
        var validationErrors = RequestValidation.Validate(request);
        if (validationErrors is not null)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var result = await useCases.RegisterAsync(
            request.Name!,
            request.Email!,
            request.Password!,
            cancellationToken: cancellationToken);
        if (!result.IsSuccess)
        {
            return ApplicationProblemMapping.ToProblem(result.Error!, httpContext.Request.Path);
        }

        return TypedResults.Created(
            (string?)null,
            new RegisterResponse(result.Value.Id, result.Value.IsActive));
    }

    private static async Task<Results<Ok<LoginResponse>, ValidationProblem, ProblemHttpResult>>
        LoginAsync(
            LoginRequest request,
            UserAuthenticationUseCases useCases,
            IAccessTokenIssuer accessTokenIssuer,
            HttpContext httpContext,
            CancellationToken cancellationToken)
    {
        var validationErrors = RequestValidation.Validate(request);
        if (validationErrors is not null)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var result = await useCases.AuthenticateAsync(
            request.Email!,
            request.Password!,
            cancellationToken);
        if (result.Status != AuthenticationStatus.Authenticated || result.User is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication failed",
                detail: "The email or password is invalid.",
                instance: httpContext.Request.Path);
        }

        var accessToken = accessTokenIssuer.Issue(result.User);
        return TypedResults.Ok(new LoginResponse(
            accessToken.Value,
            "Bearer",
            accessToken.ExpiresAtUtc));
    }

    private static Results<Ok<CurrentUserResponse>, UnauthorizedHttpResult> GetCurrentUser(
        ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not Guid userId)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(new CurrentUserResponse(userId, currentUser.Roles));
    }
}
