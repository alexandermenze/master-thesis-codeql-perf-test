using System.Net;
using System.Security.Claims;
using System.Text.Json.Serialization;
using CustomerPortal.UserAuthService.Authentication;
using CustomerPortal.UserAuthService.Domain.Aggregates;
using CustomerPortal.UserAuthService.Domain.Exceptions;
using CustomerPortal.UserAuthService.Domain.Extensions;
using CustomerPortal.UserAuthService.Domain.Services;
using CustomerPortal.UserAuthService.Postgres.Extensions;
using CustomerPortal.UserAuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;
using ThreatModel.Attributes;

namespace CustomerPortal.UserAuthService;

public static class Program
{
    [ThreatModelProcess("user-auth-service")]
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddTransient<ITokenGenerationService, TokenGenerationService>();
        builder.Services.AddSingleton(TimeProvider.System);

        builder
            .Services.AddAuthentication(TokenAuthSchemeConstants.AuthenticationScheme)
            .AddScheme<TokenAuthSchemeOptions, TokenAuthenticationHandler>(
                TokenAuthSchemeConstants.AuthenticationScheme,
                _ => { }
            );

        builder.Services.AddAuthorization(o =>
        {
            var adminPolicy = new AuthorizationPolicyBuilder()
                .RequireClaim(
                    ClaimTypes.Role,
                    UserRole.SuperAdmin.ToString(),
                    UserRole.Admin.ToString()
                )
                .Build();

            o.AddPolicy(Policies.AtLeastAdmin, adminPolicy);

            var salesDepartmentPolicy = new AuthorizationPolicyBuilder()
                .RequireClaim(
                    ClaimTypes.Role,
                    UserRole.SuperAdmin.ToString(),
                    UserRole.Admin.ToString(),
                    UserRole.SalesDepartment.ToString()
                )
                .Build();

            o.AddPolicy(Policies.AtLeastSalesDepartment, salesDepartmentPolicy);

            var customerPolicy = new AuthorizationPolicyBuilder()
                .RequireClaim(
                    ClaimTypes.Role,
                    UserRole.SuperAdmin.ToString(),
                    UserRole.Admin.ToString(),
                    UserRole.SalesDepartment.ToString(),
                    UserRole.Customer.ToString()
                )
                .Build();

            o.AddPolicy(Policies.AtLeastCustomer, customerPolicy);

            o.FallbackPolicy = adminPolicy;
        });

        builder.Services.AddUserAuthServicePostgres(o =>
            builder.Configuration.GetSection("Postgres").Bind(o)
        );
        builder.Services.AddUserAuthService(o =>
            builder.Configuration.GetSection("Auth:Admin").Bind(o)
        );

        builder.Services.AddProblemDetails(o =>
            o.CustomizeProblemDetails = ctx =>
            {
                var statusCode = ctx.Exception switch
                {
                    EntityNotFoundException => (int)HttpStatusCode.NotFound,
                    OperationConflictException => (int)HttpStatusCode.Conflict,
                    DomainValidationException => (int)HttpStatusCode.UnprocessableEntity,
                    _ => StatusCodes.Status500InternalServerError,
                };

                var title = ctx.Exception switch
                {
                    EntityNotFoundException => "Entity not found.",
                    OperationConflictException => "Conflict occurred.",
                    DomainValidationException => "Domain logic validation error.",
                    _ => "Internal server error.",
                };

                ctx.ProblemDetails.Title = title;
                ctx.HttpContext.Response.StatusCode = statusCode;
                ctx.ProblemDetails.Status = statusCode;
                ctx.ProblemDetails.Detail = ctx.Exception?.Message;
            }
        );

        builder
            .Services.AddControllers()
            .AddJsonOptions(o =>
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())
            );
        builder.Services.AddOpenApi();

        builder.Services.AddTransient<IPasswordHasher<string>, PasswordHasher<string>>();

        var app = builder.Build();

        await app.Services.InitializeUserAuthServicePostgres();
        await app.Services.InitializeUserAuthService();

        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference().AllowAnonymous();

        app.MapGet("/", [ExcludeFromDescription] () => Results.LocalRedirect("/scalar/v1"))
            .AllowAnonymous();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.MapControllers();

        await app.RunAsync();
    }
}
