using CustomerPortal.UserAuthService.Domain.Factories;
using CustomerPortal.UserAuthService.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CustomerPortal.UserAuthService.Domain.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddUserAuthService(
        this IServiceCollection services,
        Action<AdminCredentialOptions> configureOptions
    )
    {
        services.Configure(configureOptions);

        services.AddTransient<IEmailAddressValidationService, EmailAddressValidationService>();
        services.AddTransient<IPasswordService, PasswordService>();
        services.AddTransient<IRegisterUserService, RegisterUserService>();
        services.AddTransient<IUserFactory, UserFactory>();
        services.AddTransient<IAuthenticateUserService, AuthenticateUserService>();
        services.AddTransient<IUserManagementService, UserManagementService>();
        services.AddTransient<ISuperAdminSetupService, SuperAdminSetupService>();
    }

    public static async Task InitializeUserAuthService(this IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var adminCredentialOptions = scope
            .ServiceProvider.GetRequiredService<IOptions<AdminCredentialOptions>>()
            .Value;

        var superAdminSetupService =
            scope.ServiceProvider.GetRequiredService<ISuperAdminSetupService>();

        if (adminCredentialOptions is not { Email: not null, Password: not null })
            return;

        await superAdminSetupService.SetupSuperAdmin(
            adminCredentialOptions.Email,
            adminCredentialOptions.Password
        );
    }
}
