using CustomerPortal.UserAuthService.Domain.Aggregates;
using CustomerPortal.UserAuthService.Domain.Exceptions;
using CustomerPortal.UserAuthService.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace CustomerPortal.UserAuthService.Domain.Services;

public class UserManagementService(
    ILogger<UserManagementService> logger,
    IUserRepository userRepository
) : IUserManagementService
{
    public async Task<User> Approve(Guid approverGuid, Guid candidateGuid, int customerNo)
    {
        var approver = await userRepository.GetById(approverGuid);
        var candidate = await userRepository.GetById(candidateGuid);

        if (approver is null || candidate is null)
            throw new EntityNotFoundException("Current user or candidate not found.");

        if (AllowsStateChange(approver.Role, candidate.Role) is false)
            throw new DomainValidationException("Insufficient rights to approve.");

        candidate.Approve(customerNo);
        await userRepository.Save(candidate);

        logger.LogInformation(
            "Candidate {CandidateId} was approved by {ApproverId}.",
            candidateGuid,
            approverGuid
        );

        return candidate;
    }

    public async Task<User> Deactivate(Guid deactivatingPartyGuid, Guid candidateGuid)
    {
        var deactivatingParty = await userRepository.GetById(deactivatingPartyGuid);
        var candidate = await userRepository.GetById(candidateGuid);

        if (deactivatingParty is null || candidate is null)
            throw new EntityNotFoundException("Current user or candidate not found.");

        if (AllowsStateChange(deactivatingParty.Role, candidate.Role) is false)
            throw new DomainValidationException("Insufficient rights to approve.");

        candidate.Deactivate();
        await userRepository.Save(candidate);

        logger.LogInformation(
            "User {CandidateId} was deactivated by {UserId}.",
            candidateGuid,
            deactivatingPartyGuid
        );

        return candidate;
    }

    private static bool AllowsStateChange(UserRole stateChangerRole, UserRole candidateRole) =>
        (stateChangerRole, candidateRole) switch
        {
            (UserRole.SuperAdmin, _) => true,
            (UserRole.Admin, UserRole.SalesDepartment) => true,
            (UserRole.Admin, UserRole.Customer) => true,
            (UserRole.SalesDepartment, UserRole.Customer) => true,
            _ => false,
        };
}
