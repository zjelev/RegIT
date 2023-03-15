using Regit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace Regit.Authorization;

public class IsOwnerAuthorizationHandler: AuthorizationHandler<OperationAuthorizationRequirement, Contract>
{
    UserManager<IdentityUser> _userManager;

    public IsOwnerAuthorizationHandler(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    protected override Task
        HandleRequirementAsync(AuthorizationHandlerContext context,
                               OperationAuthorizationRequirement requirement,
                               Contract resource)
    {
        if (context.User == null || resource == null)
            return Task.CompletedTask;

        // If not asking for CRUD permission, return.

        if (requirement.Name != Constants.CreateOperationName &&
            requirement.Name != Constants.ReadOperationName &&
            requirement.Name != Constants.UpdateOperationName &&
            requirement.Name != Constants.DeleteOperationName)
            return Task.CompletedTask;

        if (resource.OwnerId == _userManager.GetUserId(context.User))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}