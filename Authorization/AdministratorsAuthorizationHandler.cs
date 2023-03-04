using System.Threading.Tasks;
using Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Contracts.Authorization
{
    public class AdministratorsAuthorizationHandler
                    : AuthorizationHandler<OperationAuthorizationRequirement, Contract>
    {
        protected override Task HandleRequirementAsync(
                                              AuthorizationHandlerContext context,
                                    OperationAuthorizationRequirement requirement, 
                                     Contract resource)
        {
            if (context.User == null)
                return Task.CompletedTask;

            // Administrators can do anything.
            if (context.User.IsInRole(Constants.AdministratorsRole))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}