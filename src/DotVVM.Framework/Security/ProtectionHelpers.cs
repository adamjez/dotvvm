using System;
using System.Collections.Generic;
using System.Linq;
using DotVVM.Framework.Hosting;

namespace DotVVM.Framework.Security
{
    public static class ProtectionHelpers
    {
        public static string GetRequestIdentity(DotvvmRequestContext context)
        {
            return context.HttpContext.Request.GetAbsoluteUrl().ToString();
        }

        public static string GetUserIdentity(DotvvmRequestContext context)
        {
            var user = context.HttpContext.User;
            var userIdentity = user != null && user.Identity.IsAuthenticated ? user.Identity.Name : null;
            return userIdentity;
        }
    }
}