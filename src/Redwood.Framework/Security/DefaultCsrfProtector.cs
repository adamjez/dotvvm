﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Security.Cookies.Infrastructure;
using Redwood.Framework.Hosting;

namespace Redwood.Framework.Security
{
    /// <summary>
    /// Implements synchronizer token pattern for CSRF protection.
    /// <para>The token is generated based on Session ID (random 256-bit value persisted in cookie), 
    /// Request identity (full URI) and User identity (user name, if authenticated).</para>
    /// <para>Value of stored Session ID and the token itself is encrypted and signed.</para>
    /// </summary>
    public class DefaultCsrfProtector : ICsrfProtector
    {
        private const int SID_LENGTH = 32; // 256-bit identifier
        private const string KDF_LABEL_SID = "Redwood.Framework.Security.DefaultCsrfProtector.SID"; // Key derivation label for protecting SID
        private const string KDF_LABEL_TOKEN = "Redwood.Framework.Security.DefaultCsrfProtector.Token"; // Key derivation label for protecting token

        public string GenerateToken(RedwoodRequestContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            // Get SID
            var sid = this.GetOrCreateSessionId(context);

            // Get application key helper
            var keyHelper = new ApplicationKeyHelper(context.Configuration.Security);

            // Get token
            var userIdentity = ProtectionHelpers.GetUserIdentity(context);
            var requestIdentity = ProtectionHelpers.GetRequestIdentity(context);
            var tokenData = keyHelper.ProtectData(sid, KDF_LABEL_TOKEN, userIdentity, requestIdentity);

            // Return encoded token
            return Convert.ToBase64String(tokenData);
        }

        public void VerifyToken(RedwoodRequestContext context, string token)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (string.IsNullOrWhiteSpace(token)) throw new SecurityException("CSRF protection token is missing.");

            // Get application key helper
            var keyHelper = new ApplicationKeyHelper(context.Configuration.Security);

            // Get token
            var userIdentity = ProtectionHelpers.GetUserIdentity(context);
            var requestIdentity = ProtectionHelpers.GetRequestIdentity(context);
            byte[] tokenSid;
            try
            {
                var tokenData = Convert.FromBase64String(token);
                tokenSid = keyHelper.UnprotectData(tokenData, KDF_LABEL_TOKEN, userIdentity, requestIdentity);
            }
            catch (Exception ex)
            {
                // Incorrect Base64 formatting of crypto protection error
                throw new SecurityException("CSRF protection token is invalid.", ex);
            }

            // Get SID from cookie and compare with token one
            var cookieSid = this.GetOrCreateSessionId(context);
            if (!cookieSid.SequenceEqual(tokenSid)) throw new SecurityException("CSRF protection token is invalid.");
        }

        private byte[] GetOrCreateSessionId(RedwoodRequestContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (string.IsNullOrWhiteSpace(context.Configuration.Security.SessionIdCookieName)) throw new FormatException("Configured SessionIdCookieName is missing or empty.");

            // Get cookie manager
            var mgr = new ChunkingCookieManager(); // TODO: Make this configurable

            // Get application key helper
            var keyHelper = new ApplicationKeyHelper(context.Configuration.Security);

            // Get cookie value
            var sidCookieValue = mgr.GetRequestCookie(context.HttpContext, context.Configuration.Security.SessionIdCookieName);

            if (string.IsNullOrWhiteSpace(sidCookieValue))
            {
                // No SID - generate and protect new one
                var sid = new byte[SID_LENGTH];
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    rng.GetBytes(sid);
                }
                var protectedSid = keyHelper.ProtectData(sid, KDF_LABEL_SID);

                // Save to cookie
                sidCookieValue = Convert.ToBase64String(protectedSid);
                mgr.AppendResponseCookie(
                    context.HttpContext,
                    context.Configuration.Security.SessionIdCookieName, // Configured cookie name
                    sidCookieValue,                                     // Base64-encoded SID value
                    new CookieOptions
                    {
                        HttpOnly = true,                                // Don't allow client script access
                        Secure = context.HttpContext.Request.IsHttps   // If request goes trough HTTPS, mark as secure only
                    });

                // Return newly generated SID
                return sid;
            }
            else
            {
                // Try to read from cookie
                try
                {
                    var protectedSid = Convert.FromBase64String(sidCookieValue);
                    var sid = keyHelper.UnprotectData(protectedSid, KDF_LABEL_SID);
                    return sid;
                }
                catch (Exception ex)
                {
                    // Incorrect Base64 formatting of crypto protection error
                    throw new SecurityException("Value of the SessionID cookie is corrupted or has been tampered with.", ex);
                }
            }
        }

    }
}
