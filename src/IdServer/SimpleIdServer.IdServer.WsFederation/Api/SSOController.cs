﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml;
using Microsoft.IdentityModel.Tokens.Saml2;
using SimpleIdServer.IdServer.Api;
using SimpleIdServer.IdServer.Domains;
using SimpleIdServer.IdServer.DTOs;
using SimpleIdServer.IdServer.Exceptions;
using SimpleIdServer.IdServer.Extractors;
using SimpleIdServer.IdServer.Options;
using SimpleIdServer.IdServer.Stores;
using SimpleIdServer.IdServer.WsFederation.Extensions;
using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace SimpleIdServer.IdServer.WsFederation.Api;

public class SSOController : BaseWsFederationController
{
    private readonly IClientRepository _clientRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDataProtector _dataProtector;
    private readonly IScopeClaimsExtractor _claimsExtractor;
    private readonly IdServerHostOptions _options;

    public SSOController(IClientRepository clientRepository, 
        IUserRepository userRepository,
        IDataProtectionProvider dataProtectionProvider,
        IScopeClaimsExtractor claimsExtractor,
        IOptions<IdServerHostOptions> opts, 
        IOptions<IdServerWsFederationOptions> options, 
        IKeyStore keyStore) : base(options, keyStore)
    {
        _clientRepository = clientRepository;
        _userRepository = userRepository;
        _dataProtector = dataProtectionProvider.CreateProtector("Authorization");
        _claimsExtractor = claimsExtractor;
        _options = opts.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Login([FromRoute] string prefix, CancellationToken cancellationToken)
    {
        using (var activity = Tracing.IdserverActivitySource.StartActivity("WsFederation.Login"))
        {
            var queryStr = Request.QueryString.Value;
            var federationMessage = WsFederationMessage.FromQueryString(queryStr);
            try
            {
                if (federationMessage.IsSignInMessage)
                {
                    return await SignIn(prefix, federationMessage, cancellationToken);
                }

                return RedirectToAction("EndSession", "CheckSession");
            }
            catch (OAuthException ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                return RedirectToAction("Index", "Errors", new { code = ex.Code, message = ex.Message });
            }
        }
    }

    private async Task<IActionResult> SignIn(string realm, WsFederationMessage message, CancellationToken cancellationToken)
    {
        var issuer = Request.GetAbsoluteUriWithVirtualPath();
        var client = await Validate();
        if (User == null || User.Identity == null || User.Identity.IsAuthenticated == false)
            return RedirectToLoginPage();

        var tokenType = GetTokenType(client);
        var nameIdentifier = User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
        var user = await _userRepository.GetBySubject(nameIdentifier, realm ?? Constants.DefaultRealm, cancellationToken);
        var subject = await BuildSubject(realm);
        return BuildResponse(realm);

        async Task<Domains.Client> Validate()
        {
            var str = realm ?? Constants.DefaultRealm;
            var client = await _clientRepository.GetByClientId(str, message.Wtrealm, cancellationToken);
            if (client == null)
                throw new OAuthException(ErrorCodes.INVALID_RP, Resources.Global.UnknownRp);

            if (!client.IsWsFederationEnabled())
                throw new OAuthException(ErrorCodes.INVALID_RP, Resources.Global.WsFederationNotEnabled);

            var tokenType = GetTokenType(client);
            if (tokenType != WsFederationConstants.TokenTypes.Saml2TokenProfile11 && tokenType != WsFederationConstants.TokenTypes.Saml11TokenProfile11)
                throw new OAuthException(ErrorCodes.INVALID_RP, Resources.Global.UnsupportedTokenType);

            return client;
        }

        IActionResult RedirectToLoginPage()
        {
            var queryStr = Request.QueryString.Value;
            if (!string.IsNullOrEmpty(realm))
                issuer = $"{issuer}/{realm}";

            var returnUrl = $"{issuer}/{WsFederationConstants.EndPoints.SSO}{queryStr}&{AuthorizationRequestParameters.ClientId}={message.Wtrealm}";
            var url = Url.Action("Index", "Authenticate", new
            {
                returnUrl = _dataProtector.Protect(returnUrl),
                area = Constants.AreaPwd
            });
            return Redirect(url);
        }

        async Task<ClaimsIdentity> BuildSubject(string realm)
        {
            var context = new HandlerContext(new HandlerContextRequest(Request.GetAbsoluteUriWithVirtualPath(), string.Empty, null, null, null, (X509Certificate2)null, null), realm ?? Constants.DefaultRealm, _options);
            context.SetUser(user, null);
            var claims = (await _claimsExtractor.ExtractClaims(context, client.Scopes, ScopeProtocols.SAML)).Select(c => new Claim(c.Key, c.Value.ToString())).ToList();
            if (claims.Count(t => t.Type == ClaimTypes.NameIdentifier) == 0)
                throw new OAuthException(ErrorCodes.INVALID_RP, Resources.Global.NoClaim);

            if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Name));

            var format = Microsoft.IdentityModel.Tokens.Saml2.ClaimProperties.SamlNameIdentifierFormat;
            if (tokenType == WsFederationConstants.TokenTypes.Saml11TokenProfile11)
                format = Microsoft.IdentityModel.Tokens.Saml.ClaimProperties.SamlNameIdentifierFormat;

            foreach (var cl in claims)
            {
                if (cl.Type == ClaimTypes.NameIdentifier)
                    cl.Properties[format] = Options.DefaultNameIdentifierFormat;
            }

            if (claims.Count() == 1 && !claims.Any(c => c.Type == ClaimTypes.Name))
                claims.Add(new Claim(ClaimTypes.Name, user.Name));

            return new ClaimsIdentity(claims, "idserver");
        }

        IActionResult BuildResponse(string realm)
        {
            var descriptor = new SecurityTokenDescriptor
            {
                Audience = client.ClientId,
                IssuedAt = DateTime.UtcNow,
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddSeconds(client.TokenExpirationTimeInSeconds ?? _options.DefaultTokenExpirationTimeInSeconds),
                Subject = subject,
                Issuer = issuer,
                SigningCredentials = GetSigningCredentials(realm ?? Constants.DefaultRealm)
            };
            SecurityTokenHandler handler;
            if (tokenType == WsFederationConstants.TokenTypes.Saml2TokenProfile11)
                handler = new Saml2SecurityTokenHandler();
            else
                handler = new SamlSecurityTokenHandler();

            var securityToken = handler.CreateToken(descriptor);

            var response = new RequestSecurityTokenResponse
            {
                CreatedAt = securityToken.ValidFrom,
                ExpiresAt = securityToken.ValidTo,
                AppliesTo = client.ClientId,
                Context = message.Wctx,
                RequestedSecurityToken = securityToken,
                SecurityTokenHandler = handler
            };

            var responseMessage = new WsFederationMessage
            {
                IssuerAddress = message.Wreply,
                Wresult = response.Serialize(),
                Wctx = message.Wctx,
                PostTitle = message.PostTitle,
                Script = message.Script,
                ScriptButtonText = message.ScriptButtonText,
                ScriptDisabledText = message.ScriptDisabledText,
                Wa = Microsoft.IdentityModel.Protocols.WsFederation.WsFederationConstants.WsFederationActions.SignIn
            };

            if (!string.IsNullOrWhiteSpace(message.Script))
            {
                var content = responseMessage.BuildFormPost();
                return new ContentResult
                {
                    ContentType = "text/html",
                    StatusCode = (int)HttpStatusCode.OK,
                    Content = responseMessage.BuildFormPost()
                };
            }

            return Redirect(responseMessage.BuildRedirectUrl());
        }
    }

    private string GetTokenType(Client client) => client.GetWsTokenType() ?? Options.DefaultTokenType;
}
