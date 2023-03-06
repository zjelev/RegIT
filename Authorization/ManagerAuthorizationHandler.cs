using Regit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Regit.Authorization;

public class ManagerAuthorizationHandler: AuthorizationHandler<OperationAuthorizationRequirement, Contract>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement, Contract resource)
    {
        if (context.User == null || resource == null)
            return Task.CompletedTask;

        // If not asking for approval/reject, return.
        if (requirement.Name != Constants.ApproveOperationName &&
            requirement.Name != Constants.RejectOperationName)
            return Task.CompletedTask;

        // Managers can approve or reject.
        if (context.User.IsInRole(Constants.ManagersRole))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}