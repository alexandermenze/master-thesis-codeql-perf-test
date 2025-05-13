namespace CustomerPortal.UserAuthService.Domain.Services;

public interface ISuperAdminSetupService
{
    Task SetupSuperAdmin(string email, string password);
}
