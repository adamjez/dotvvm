using System;
using System.Collections.Generic;
using System.Linq;
using Redwood.Framework.Hosting;

namespace Redwood.Framework.Security
{
    public static class ProtectionHelpers
    {
        public static string GetRequestIdentity(RedwoodRequestContext context)
        {
            return context.HttpContext.Request.GetAbsoluteUrl().ToString();
        }

        public static string GetUserIdentity(RedwoodRequestContext context)
        {
            var user = context.HttpContext.User;
            var userIdentity = user != null && user.Identity.IsAuthenticated ? user.Identity.Name : null;
            return userIdentity;
        }
    }
}