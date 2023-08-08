using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace secondTask.errorHandling
{
    public class CustomAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (!ShouldSkipAuthorization(context))
            {
                if (!IsAuthorized(context))
                {
                    // Return a custom unauthorized response 
                    context.Result = new JsonResult(new { message = "You are not authorized to access this resource." })
                    {
                        StatusCode = StatusCodes.Status401Unauthorized
                    };
                }
            }
        }

        private bool ShouldSkipAuthorization(AuthorizationFilterContext context)
        {
            var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;

            if (actionDescriptor != null)
            {
                // List the action names that should be excluded from authorization
                var excludedActions = new[] { "Register", "Login" };
                return excludedActions.Contains(actionDescriptor.ActionName);
            }

            return false; // Continue authorization for other actions
        }

        private bool IsAuthorized(AuthorizationFilterContext context)
        {
            // Check if the user is authenticated
            if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                return true; // User is authorized
            }

            return false; // User is not authorized
        }
    }
}
