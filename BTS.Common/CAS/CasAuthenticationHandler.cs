using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Globalization;
using System.Net;
using System.Runtime.Caching;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Owin;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;

namespace BTS.Common.CAS
{
    /// <summary>
    /// Middleware to handle CAS authentication
    /// </summary>
    public class CasAuthenticationHandler : AuthenticationHandler<CasAuthenticationOptions>
    {
        private string GetServiceUrl(string returnUrl)
        {
            // determine service URL
            string baseUri = Request.Scheme
                + Uri.SchemeDelimiter
                + Request.Host
                + Request.PathBase
                + Options.CallbackPath;

            if (!String.IsNullOrEmpty(returnUrl))
            {
                baseUri += new QueryString(Constants.RETURN_URL, returnUrl);
            }

            return baseUri;
        }

        private string GetProxyCallbackUrl()
        {
            return Request.Scheme + Uri.SchemeDelimiter + Request.Host + Request.PathBase + Options.ProxyCallback;
        }

        /// <summary>
        /// Redirect user to CAS
        /// </summary>
        /// <returns></returns>
        protected override Task ApplyResponseChallengeAsync()
        {
            if (Response.StatusCode == 401)
            {
                if (Request.User.Identity.IsAuthenticated)
                {
                    if (Options.NoPermsPath.HasValue)
                    {
                        Response.Redirect(Request.PathBase.ToString() + Options.NoPermsPath.ToString());
                    }
                    else
                    {
                        // to avoid redirect loops, change the status to 403
                        Response.StatusCode = 403;
                    }
                }
                else
                {
                    var challenge = Helper.LookupChallenge(Options.AuthenticationType, Options.AuthenticationMode);

                    if (challenge != null)
                    {
                        // determine query string params to pass to CAS
                        var queryParams = new Dictionary<string, string>()
                        {
                            { "service", GetServiceUrl(challenge.Properties.RedirectUri) }
                        };

                        if (Options.Renew)
                        {
                            queryParams.Add("renew", "true");
                        }
                        else if (Options.Gateway)
                        {
                            queryParams.Add("gateway", "true");
                        }

                        if (!String.IsNullOrEmpty(Options.Method))
                        {
                            queryParams.Add("method", Options.Method);
                        }

                        Response.Redirect(WebUtilities.AddQueryString(new Uri(Options.CasUrlBase, "login").ToString(), queryParams));
                    }
                }
            }

            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Validate CAS auth
        /// </summary>
        /// <returns></returns>
        public override async Task<bool> InvokeAsync()
        {
            // our CallbackPath is called during a logout operation as well, and we want to ensure that we don't blow off existing auth info
            // during a logout so that we can successfully access the user context in logout code
            if (Request.Path == Options.CallbackPath && (!String.IsNullOrEmpty(Request.Query["ticket"]) || !String.IsNullOrEmpty(Request.Query["pgtId"])))
            {
                return await HandleLoginAsync();
            }
            else if (Request.Path == Options.CallbackPath && Request.Method.ToUpperInvariant() == "POST")
            {
                return await HandleLogoutAsync();
            }
            else if (Request.Path == Options.ImpersonationPath)
            {
                return await HandleImpersonateAsync();
            }

            return false;
        }

        private async Task<bool> HandleLoginAsync()
        {
            var ticket = await AuthenticateAsync();

            if (ticket != null)
            {
                var identity = ticket.Identity;
                bool hasReturnUrl = !String.IsNullOrEmpty(Request.Query[Constants.RETURN_URL]) && Request.Query[Constants.RETURN_URL][0] == '/';

                if (ticket.Identity.AuthenticationType != Options.SignInAsAuthenticationType)
                {
                    identity = new ClaimsIdentity(ticket.Identity.Claims, Options.SignInAsAuthenticationType, ticket.Identity.NameClaimType, ticket.Identity.RoleClaimType);
                }

                Context.Authentication.SignIn(ticket.Properties, identity);

                if (Options.LoginPath.HasValue)
                {
                    var queryParams = new Dictionary<string, string>();
                    if (hasReturnUrl)
                    {
                        queryParams[Constants.RETURN_URL] = Request.Query[Constants.RETURN_URL];
                    }

                    Context.Response.Redirect(WebUtilities.AddQueryString(Request.PathBase.ToString() + Options.LoginPath.ToString(), queryParams));
                }
                else if (hasReturnUrl)
                {
                    Context.Response.Redirect(Request.Query[Constants.RETURN_URL]);
                }
                else
                {
                    var pathBase = Request.PathBase.ToString();
                    if (String.IsNullOrEmpty(pathBase))
                    {
                        pathBase = "/";
                    }

                    Context.Response.Redirect(pathBase);
                }

                return true;
            }

            return false;
        }

        private async Task<bool> HandleLogoutAsync()
        {
            string ticket = null;
            var form = await Request.ReadFormAsync();
            var logoutRequest = form["logoutRequest"];

            if (logoutRequest == null)
            {
                // not a logout request
                return false;
            }

            try
            {
                using (var reader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(logoutRequest))))
                {
                    // we may want to do further validation here, such as verifying that
                    // the IssueInstant is within allowable skew. However, since this is guarding
                    // a logout, the worst security issue that can occur is forcing a minor DOS in the sense
                    // that the user will renew their SSO session next time they load a page
                    // (meaning longer pageload times). In all, not a huge deal.
                    reader.MoveToContent();
                    if (reader.ReadToDescendant("samlp:SessionIndex"))
                    {
                        ticket = reader.ReadElementContentAsString();
                    }
                }

                if (!String.IsNullOrEmpty(ticket))
                {
                    var properties = new AuthenticationProperties();
                    properties.Dictionary[Constants.SERVICE_TICKET] = ticket;

                    // this might not actually do anything, we ensure we kill the session from our session manager as well
                    Context.Authentication.SignOut(properties, Options.AuthenticationType);
                    await Options.SessionStore?.RemoveAsync(ticket);

                    return true;
                }
            }
            catch (XmlException)
            {
                // no-op; was a POST but not valid XML so probably not a SLO request from CAS
            }

            return false;
        }

        private async Task<bool> HandleImpersonateAsync()
        {
            // TODO: FINISH THIS
            /* Need to:
             * 1. If user is not authenticated, reject. It is expected that the user will log in and then click something in UI to impersonate
             * 2. Once we have a user, fire CanImpersonate to determine if the user is allowed to impersonate
             * 3. If valid, go through auth flow with impersonated user (no owin sign-in yet here either)
             * 4. Fire OnImpersonate to alert app that impersonation is happening. It can use this to modify claims before we save them
             * 5. Sign them into owin (destroying existing user session in the process -- need to ensure this sets cookies properly)
             * 6. Also build a way to drop impersonation and go back to regular user account somehow
             * (actually pretty easy, just blow off impersonation session, which forces them to sign in again, which will be as themselves)
             */
            return false;
        }


        /// <summary>
        /// Validate ST and set up user
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings",
            Justification = "Uris are used when possible, but adding query strings to them is a nightmare")]
        protected override async Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            string ticket = Request.Query["ticket"];
            string pgtId = Request.Query["pgtId"];
            string pgtIou = Request.Query["pgtIou"];
            string returnUrl = Request.Query[Constants.RETURN_URL];

            if (pgtId != null && pgtIou != null)
            {
                // we have a proxy granting ticket, need to store this somehow so it's available in a future request
                // Note: This can be a DOS vector to exhaust the memory of the machine; it is recommended to configure the memoryCache
                // in the config in order to ensure that someone spamming with these params cannot do that
                MemoryCache.Default.Add(pgtIou, pgtId, DateTime.Now.AddMinutes(5));
                return null;
            }
            else if (ticket == null)
            {
                return null;
            }

            // we have a service ticket, validate it with CAS
            var casUri = Options.CasUrlBase;

            casUri = Options.CasVersion switch
            {
                1 => new Uri(casUri, "validate"),
                2 => new Uri(casUri, Options.ProxyClient ? "proxyValidate" : "serviceValidate"),
                3 => new Uri(casUri, Options.ProxyClient ? "p3/proxyValidate" : "p3/serviceValidate"),
                _ => throw new InvalidOperationException("Unrecognized CAS protocol version, only versions 1, 2, and 3 are supported"),
            };
            var queryParams = new Dictionary<string, string>()
            {
                { "service", GetServiceUrl(returnUrl) },
                { "ticket", ticket }
            };

            if (Options.Renew)
            {
                queryParams.Add("renew", "true");
            }

            if (Options.ProxyServer)
            {
                queryParams.Add("pgtUrl", GetProxyCallbackUrl());
            }

            var validationRequest = WebRequest.CreateHttp(WebUtilities.AddQueryString(casUri.ToString(), queryParams));
            var validationResponse = await validationRequest.GetResponseAsync();
            ServiceResponseType response;

            if (Options.CasVersion == 1)
            {
                // v1 doesn't use xml
                using var stream = new StreamReader(validationResponse.GetResponseStream());
                var success = stream.ReadLine();
                if (success == "no")
                {
                    return null;
                }

                // build up a v2 response object so we can avoid code duplication
                response = new ServiceResponseType()
                {
                    Item = new AuthenticationSuccessType()
                    {
                        user = stream.ReadLine()
                    }
                };
            }
            else
            {
                using var stream = XmlReader.Create(validationResponse.GetResponseStream());
                var serializer = new XmlSerializer(typeof(ServiceResponseType));
                response = serializer.Deserialize(stream) as ServiceResponseType;
            }

            if (response.Item is AuthenticationFailureType failure)
            {
                // further details can be found in failure.code and failure.Value
                return null;
            }
            else if (response.Item is AuthenticationSuccessType success)
            {
                var identity = new ClaimsIdentity(Constants.CAS, ClaimTypes.NameIdentifier, ClaimTypes.Role);
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, success.user));
                identity.AddClaim(new Claim(Constants.IDENTITY_CLAIM, "CAS"));
                identity.AddClaim(new Claim(Constants.SERVICE_TICKET, ticket));

                var properties = new AuthenticationProperties()
                {
                    AllowRefresh = true,
                };

                // Note: it is harmless to say we're a proxy client even if we aren't
                if (success.proxies != null && success.proxies.Length > 0)
                {
                    if (!Options.ProxyClient)
                    {
                        throw new InvalidOperationException("Application is not registered as a CAS proxy client, but our service ticket was served by a proxy");
                    }

                    foreach (string proxy in success.proxies)
                    {
                        if (Options.TrustedProxies?.Contains(proxy) != true)
                        {
                            throw new InvalidOperationException("CAS proxy chain contains an unauthorized proxy");
                        }
                    }
                }

                properties.Dictionary.Add(Constants.SERVICE_TICKET, ticket);

                pgtIou = null;
                pgtId = null;

                if (Options.CasVersion > 1 && Options.ProxyServer)
                {
                    if (success.proxyGrantingTicket == null)
                    {
                        throw new InvalidOperationException("Application is registered as CAS proxy server, but CAS endpoint did not return a proxy granting ticket");
                    }

                    pgtIou = success.proxyGrantingTicket;
                    pgtId = (string)MemoryCache.Default[pgtIou];

                    if (pgtId == null)
                    {
                        // invalid IOU or the cache item was evicted
                        // force a re-auth
                        return null;
                    }

                    // we don't need it in cache anymore
                    properties.Dictionary.Add(Constants.PROXY_GRANTING_TICKET, pgtId);
                    MemoryCache.Default.Remove(pgtIou);
                }

                if (success.attributes != null)
                {
                    if (success.attributes.memberOf != null)
                    {
                        foreach (string role in success.attributes.memberOf)
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, role));
                        }
                    }

                    properties.Dictionary.Add("IsFromNewLogin", success.attributes.isFromNewLogin.ToString());
                    properties.IsPersistent = success.attributes.longTermAuthenticationRequestTokenUsed;
                    properties.IssuedUtc = success.attributes.authenticationDate;

                    if (success.attributes.Any != null)
                    {
                        foreach (var attr in success.attributes.Any)
                        {
                            var key = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(attr.LocalName);
                            if (properties.Dictionary.ContainsKey(key))
                            {
                                // we can get multiple attributes of the same node, since properties.Dictionary
                                // only allows string values, we flatten them into comma-separated
                                properties.Dictionary[key] += ',' + attr.InnerText;
                            }
                            else
                            {
                                properties.Dictionary.Add(key, attr.InnerText);
                            }
                        }
                    }
                }

                var context = new CasAuthenticationContext()
                {
                    Handler = this,
                    CasIdentity = identity,
                    Properties = properties,
                    ServiceTicket = properties.Dictionary[Constants.SERVICE_TICKET],
                    ProxyGrantingTicket = pgtId,
                    ProxyGrantingTicketIOU = pgtIou
                };

                try
                {
                    Options.OnAuthenticated?.Invoke(context);

                    var list = new IdentityList(context.OtherIdentities);
                    list.Items.Add(context.CasIdentity);
                    context.Properties.Dictionary[Constants.IDENTITY_PROP] = list.Serialize();

                    var cookieIdentity = new ClaimsIdentity(Options.SignInAsAuthenticationType);
                    cookieIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, context.CasIdentity.Name));
                    cookieIdentity.AddClaim(new Claim(ClaimTypes.Name, context.CasIdentity.Name));
                    cookieIdentity.AddClaim(new Claim(Constants.IDENTITY_CLAIM, "CAS"));

                    Options.OnMakeClaims?.Invoke(context, cookieIdentity);

                    return new AuthenticationTicket(cookieIdentity, context.Properties);
                }
                catch (NotAuthorizedException)
                {
                    // OnAuthenticated or OnMakeClaims indicated that this is not a valid user account
                    return null;
                }
            }
            else
            {
                // unknown response
                return null;
            }
        }

        [SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings",
            Justification = "Uris are used when possible, but adding query strings to them is a nightmare")]
        internal async Task<string> GetProxyServiceTicket(CasAuthenticationContext context, string service)
        {
            var casUri = Options.CasUrlBase;

            if (!Options.ProxyServer)
            {
                throw new InvalidOperationException("Application is not registered as a CAS proxy server");
            }

            if (String.IsNullOrEmpty(context.ProxyGrantingTicket))
            {
                throw new InvalidOperationException("Invalid Proxy Granting Ticket");
            }

            switch (Options.CasVersion)
            {
                case 1:
                    throw new InvalidOperationException("Proxying is not supported with CAS protocol version 1");
                case 2:
                case 3:
                    casUri = new Uri(casUri, "proxy");
                    break;
                default:
                    throw new InvalidOperationException("Unrecognized CAS protocol version, only versions 1, 2, and 3 are supported");
            }

            var queryParams = new Dictionary<string, string>()
            {
                { "targetService", service },
                { "pgt", context.ProxyGrantingTicket }
            };

            var proxyRequest = WebRequest.CreateHttp(WebUtilities.AddQueryString(casUri.ToString(), queryParams));

            using var proxyResponse = await proxyRequest.GetResponseAsync();
            using var stream = XmlReader.Create(proxyResponse.GetResponseStream());
            var serializer = new XmlSerializer(typeof(ServiceResponseType));
            var response = serializer.Deserialize(stream) as ServiceResponseType;

            if (response.Item is ProxyFailureType failure)
            {
                // failure.code and failure.Value contain additional details
                return null;
            }
            else if (response.Item is ProxySuccessType success)
            {
                return success.proxyTicket;
            }
            else
            {
                // unexpected response
                return null;
            }
        }
    }
}
