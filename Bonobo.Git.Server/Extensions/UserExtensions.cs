﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace Bonobo.Git.Server
{
    public static class UserExtensions
    {
        static string GetClaimValue(this IPrincipal user, string claimName)
        {
            try
            {
                ClaimsIdentity claimsIdentity = GetClaimsIdentity(user);
                if (claimsIdentity != null)
                {
                    var claim = claimsIdentity.FindFirst(claimName);
                    if (claim != null)
                    {
                        return claim.Value;
                    }
                }
            }
            catch(Exception ex)
            {
                Trace.TraceError("GetClaimValue Exception " + ex);
            }
            return null;
        }

        public static Guid Id(this IPrincipal user)
        {
            string id = user.GetClaimValue(ClaimTypes.NameIdentifier);
            Guid result;
            if (Guid.TryParse(id, out result))
            {
                // It's a normal string Guid
                return result;
            }
            else if (String.IsNullOrEmpty(id))
            {
                // I think this is a disaster if we don't have this claim and we should probably throw an exception here
                // But anyway, for now I'm going to return Guid.Empty, and someone else can suffer later
                // I'm going to log it, so that when they have a NullReferenceException later, they'll be able to see that
                // this was a precursor
                Trace.TraceError("User did not have NameIdentififer claim!!!");
                return Guid.Empty;
            }
            else
            {
                try
                {
                    // We might be a ADFS-style Guid is which a base64 string
                    // If this fails, we'll get a FormatException thrown anyway
                    return new Guid(Convert.FromBase64String(id));
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Could not parse id '{0}' from NameIdentifier claim: {1}", id, ex.Message);
                    return Guid.Empty;
                }
            }
        }

        public static string Username(this IPrincipal user)
        {
            // We can tolerate the username being in either Upn or Name
            return user.GetClaimValue(ClaimTypes.Name) ?? user.GetClaimValue(ClaimTypes.Upn);
        }

        public static string DisplayName(this IPrincipal user)
        {
            return string.Format("{0} {1}", user.GetClaimValue(ClaimTypes.GivenName), user.GetClaimValue(ClaimTypes.Surname));
        }

        public static bool IsWindowsAuthenticated(this IPrincipal user)
        {
            string authenticationMethod = user.GetClaimValue(ClaimTypes.AuthenticationMethod);
            return !String.IsNullOrEmpty(authenticationMethod) && authenticationMethod.Equals("Windows", StringComparison.OrdinalIgnoreCase);
        }

        public static string[] Roles(this IPrincipal user)
        {
            string[] result = null;

            try
            {
                ClaimsIdentity claimsIdentity = GetClaimsIdentity(user);
                if (claimsIdentity != null)
                {
                    result = claimsIdentity.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray();
                }
            }
            catch(Exception ex)
            {
                Trace.TraceError("GetClaim Exception " + ex);
            }

            return result;
        }

        private static ClaimsIdentity GetClaimsIdentity(this IPrincipal user)
        {
            ClaimsIdentity result = null;

            ClaimsPrincipal claimsPrincipal = user as ClaimsPrincipal;
            if (claimsPrincipal != null)
            {
                result = claimsPrincipal.Identities.FirstOrDefault(x => x != null);
            }

            return result;
        }

        public static string StripDomain(this string username)
        {
            int delimiterIndex = username.IndexOf('@');
            if (delimiterIndex > 0)
            {
                username = username.Substring(0, delimiterIndex);
            }
            delimiterIndex = username.IndexOf('\\');
            if (delimiterIndex > 0)
            {
                username = username.Substring(delimiterIndex + 1);
            }

            return username;
        }

        public static string GetDomain(this string username)
        {
            int deliIndex = username.IndexOf('@');
            if (deliIndex > 0)
            {
                return username.Substring(deliIndex + 1);
            }

            deliIndex = username.IndexOf('\\');
            if (deliIndex > 0)
            {
                return username.Substring(0, deliIndex);
            }

            return string.Empty;
        }
    }
}