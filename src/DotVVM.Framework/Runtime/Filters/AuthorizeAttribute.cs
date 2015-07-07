using System;
using System.Collections.Generic;
using System.Linq;
using DotVVM.Framework.Hosting;

namespace DotVVM.Framework.Runtime.Filters
{
    /// <summary>
    /// A filter that checks the authorize attributes and redirects to the login page.
    /// </summary>
    public class AuthorizeAttribute : ActionFilterAttribute
    {

        /// <summary>
        /// Gets or sets the comma-separated list of roles.
        /// </summary>
        public string[] Roles { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class.
        /// </summary>
        public AuthorizeAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class.
        /// </summary>
        /// <param name="roles">The comma-separated list of roles. The user must be at least in one of them.</param>
        public AuthorizeAttribute(string roles)
        {
            Roles = roles.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
        }

        /// <summary>
        /// Called before the command is invoked.
        /// </summary>
        protected internal override void OnCommandExecuting(DotvvmRequestContext context, ActionInfo actionInfo)
        {
            Authorize(context);
            base.OnCommandExecuting(context, actionInfo);
        }

        protected internal override void OnViewModelCreated(DotvvmRequestContext context)
        {
            Authorize(context);
            base.OnViewModelCreated(context);
        }

        public void Authorize(DotvvmRequestContext context)
        {
            // the user must not be anonymous
            if (context.HttpContext.User == null || !context.HttpContext.User.Identity.IsAuthenticated)
            {
                throw new UnauthorizedAccessException();
            }

            // if the role is set
            if (Roles != null && Roles.Length > 0)
            {
                if (!Roles.Any(r => context.HttpContext.User.IsInRole(r)))
                {
                    throw new UnauthorizedAccessException();
                }
            }
        }
    }
}