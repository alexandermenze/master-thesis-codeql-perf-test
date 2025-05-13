using System.Security.Claims;
using CustomerPortal.Messages.Dtos;
using CustomerPortal.UserAuthService.Authentication;
using CustomerPortal.UserAuthService.Domain.Aggregates;
using CustomerPortal.UserAuthService.Domain.DataClasses;
using CustomerPortal.UserAuthService.Domain.Exceptions;
using CustomerPortal.UserAuthService.Domain.Extensions;
using CustomerPortal.UserAuthService.Domain.Repositories;
using CustomerPortal.UserAuthService.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreatModel.Attributes;

namespace CustomerPortal.UserAuthService.Controllers;

[ApiController]
[Route("users")]
public class UserController(
    IUserRepository userRepository,
    IRegisterUserService registerUserService,
    IAuthenticateUserService authenticateUserService,
    IUserManagementService userManagementService
) : ControllerBase
{
    [HttpGet]
    [Authorize(Policies.AtLeastSalesDepartment)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserResponseDto>))]
    public async Task<IActionResult> GetUsers()
    {
        var users = await userRepository.GetAll();
        var userResponseDtos = users.Select(u => u.ToDto());
        return Ok(userResponseDtos);
    }

    [HttpGet("unapproved")]
    [Authorize(Policies.AtLeastSalesDepartment)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserResponseDto>))]
    public async Task<IActionResult> GetUnapprovedUsers()
    {
        var users = await userRepository.GetAllPendingApproval();
        var userResponseDtos = users.Select(u => u.ToDto());
        return Ok(userResponseDtos);
    }

    [HttpGet("me")]
    [Authorize(Policies.AtLeastCustomer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserResponseDto>))]
    public async Task<IActionResult> GetMe()
    {
        var currentUserGuidString = HttpContext
            .User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        if (Guid.TryParse(currentUserGuidString, out var currentUserGuid) is false)
            throw new EntityNotFoundException("Current user not found.");

        var me = await userRepository.GetById(currentUserGuid);

        return Ok(me.ToDto());
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policies.AtLeastSalesDepartment)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var user = await userRepository.GetById(id);
        var userResponse = user.ToDto();

        if (userResponse is null)
            return NotFound();

        return Ok(userResponse);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserResponseDto))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> Register([FromBody] RegisterUserData data)
    {
        var user = await registerUserService.RegisterExternal(data);
        var userResponse = user.ToDto();
        return CreatedAtAction(nameof(Get), new { id = user.Id }, userResponse);
    }

    [HttpPost("register-customer")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserResponseDto))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> RegisterCustomer([FromBody] RegisterCustomerUserDto dto)
    {
        var user = await registerUserService.RegisterExternal(
            new RegisterUserData(
                dto.Email,
                dto.Password,
                dto.FirstName,
                dto.LastName,
                UserRole.Customer
            )
        );
        var userResponse = user.ToDto();
        return CreatedAtAction(nameof(Get), new { id = user.Id }, userResponse);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TokenResponseDto))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto data)
    {
        var result = await authenticateUserService.Login(data.Email, data.Password);

        if (result.HasValue is false)
            throw new EntityNotFoundException("User not found.");

        var (user, sessionToken) = result.Value;

        return Ok(new TokenResponseDto(user.Id, sessionToken.Token, sessionToken.ExpiresAt));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policies.AtLeastSalesDepartment)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> Approve(Guid id, ApproveCustomerDto dto)
    {
        var currentUserGuidString = HttpContext
            .User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        if (Guid.TryParse(currentUserGuidString, out var currentUserGuid) is false)
            throw new EntityNotFoundException("Current user not found.");

        var user = await userManagementService.Approve(currentUserGuid, id, dto.CustomerNo);

        return Ok(user.ToDto());
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policies.AtLeastAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var currentUserGuidString = HttpContext
            .User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        if (Guid.TryParse(currentUserGuidString, out var currentUserGuid) is false)
            throw new EntityNotFoundException("Current user not found.");

        var user = await userManagementService.Deactivate(currentUserGuid, id);

        return Ok(user.ToDto());
    }
}
