﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace SimpleIdServer.IdServer.Saml.Sp;

public class SamlSpHandler : RemoteAuthenticationHandler<SamlSpOptions>, IAuthenticationSignOutHandler
{
    private EntityDescriptor _entityDescriptor;

    public SamlSpHandler(IOptionsMonitor<SamlSpOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    protected new SamlSpEvents Events
    {
        get { return (SamlSpEvents)base.Events; }
        set { base.Events = value; }
    }

    public async Task SignOutAsync(AuthenticationProperties? properties)
    {
        if (Context.User?.Identity == null || !Context.User.Identity.IsAuthenticated) return;
        var saml2Configuration = await GetSpConfiguration();
        var binding = new Saml2PostBinding();
        var saml2LogoutRequest = new Saml2LogoutRequest(saml2Configuration, Context.User);
        await Context.SignOutAsync();
        var postBinding = binding.Bind(saml2LogoutRequest);
        var payload = Encoding.UTF8.GetBytes(postBinding.PostContent);
        await Context.Response.Body.WriteAsync(payload, 0, payload.Length);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var saml2Configuration = await GetSpConfiguration();
        var binding = new Saml2RedirectBinding();
        binding.Bind(new Saml2AuthnRequest(saml2Configuration));
        var redirectionContext = new RedirectContext<SamlSpOptions>(
                Context,
                Scheme,
                Options,
                properties,
                binding.RedirectLocation.OriginalString);
        await Events.RedirectToSsoEndpoint(redirectionContext);
    }

    protected override async Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
    {
        if (!HttpMethods.IsPost(Request.Method) && !HttpMethods.IsGet(Request.Method)) return HandleRequestResult.Fail("HTTP Method is not supported");
        var entityDescriptor = await GetEntityDescriptor(CancellationToken.None);
        var signingCertificate = entityDescriptor.IdPSsoDescriptor.SigningCertificates.First();
        var idpConfiguration = new Saml2Configuration
        {
            Issuer = Options.SPId,
            SignAuthnRequest = signingCertificate != null,
            RevocationMode = Options.RevocationMode,
            CertificateValidationMode = Options.CertificateValidationMode,
        };
        idpConfiguration.AllowedAudienceUris.Add(Options.SPId);
        idpConfiguration.AllowedIssuer = Options.IdpId;
        idpConfiguration.ArtifactResolutionService = entityDescriptor.IdPSsoDescriptor.ArtifactResolutionServices.Select(s => new Saml2IndexedEndpoint { Index = s.Index, Location = s.Location }).FirstOrDefault();
        idpConfiguration.SignatureValidationCertificates.Add(signingCertificate);

        if (HttpMethods.IsPost(Request.Method))
        {
            var binding = new Saml2PostBinding();
            var saml2AuthnResponse = new Saml2AuthnResponse(idpConfiguration);
            binding.ReadSamlResponse(Request.ToGenericHttpRequest(), saml2AuthnResponse);
            if(saml2AuthnResponse.Status != Saml2StatusCodes.Success) return HandleRequestResult.Fail("An error occured while trying to parse the SAML2 Response");
            try
            {
                binding.Unbind(Request.ToGenericHttpRequest(), saml2AuthnResponse);
                var claimsPrincipal = new ClaimsPrincipal(saml2AuthnResponse.ClaimsIdentity);
                var authenticationTicket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
                return HandleRequestResult.Success(authenticationTicket);
            }
            catch(Exception ex)
            {
                Logger.LogError(ex.ToString());
                return HandleRequestResult.Fail(ex.Message);
            }
        }

        if(HttpMethods.IsGet(Request.Method))
        {
            var spConfiguration = new Saml2Configuration
            {
                Issuer = Options.SPId,
                AllowedIssuer = entityDescriptor.EntityId,
                SingleSignOnDestination = entityDescriptor.IdPSsoDescriptor.SingleSignOnServices.First().Location,
                SingleLogoutDestination = entityDescriptor.IdPSsoDescriptor.SingleLogoutServices.First().Location,
                SigningCertificate = Options.SigningCertificate,
                SignAuthnRequest = Options.SigningCertificate != null
            };
            var binding = new Saml2ArtifactBinding();
            try
            {
                var saml2ArtifactResolve = new Saml2ArtifactResolve(idpConfiguration);
                binding.Unbind(Request.ToGenericHttpRequest(), saml2ArtifactResolve);
                saml2ArtifactResolve.Config.SigningCertificate = Options.SigningCertificate;
                saml2ArtifactResolve.Config.SignAuthnRequest = Options.SigningCertificate != null;

                var soapEnvelope = new Saml2SoapEnvelope();
                var saml2AuthnResponse = new Saml2AuthnResponse(idpConfiguration);
                await soapEnvelope.ResolveAsync(new DefaultHttpClientFactory(Options.Backchannel), saml2ArtifactResolve, saml2AuthnResponse); 
                if (saml2AuthnResponse.Status != Saml2StatusCodes.Success) return HandleRequestResult.Fail($"SAML Response status: {saml2AuthnResponse.Status}");
                var claimsPrincipal = new ClaimsPrincipal(saml2AuthnResponse.ClaimsIdentity);
                var authenticationTicket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
                return HandleRequestResult.Success(authenticationTicket);

            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
                return HandleRequestResult.Fail(ex.Message);
            }
        }

        return HandleRequestResult.Fail("Not supported");
    }

    private async Task<Saml2Configuration> GetSpConfiguration()
    {
        var entityDescriptor = await GetEntityDescriptor(CancellationToken.None);
        var saml2Configuration = new Saml2Configuration
        {
            Issuer = Options.SPId,
            AllowedIssuer = entityDescriptor.EntityId,
            SingleSignOnDestination = entityDescriptor.IdPSsoDescriptor.SingleSignOnServices.First().Location,
            SingleLogoutDestination = entityDescriptor.IdPSsoDescriptor.SingleLogoutServices.First().Location,
            SigningCertificate = Options.SigningCertificate,
            SignAuthnRequest = Options.SigningCertificate != null
        };
        return saml2Configuration;
    }

    private async Task<EntityDescriptor> GetEntityDescriptor(CancellationToken cancellationToken)
    {
        if (_entityDescriptor != null) return _entityDescriptor;
        var httpClient = Options.Backchannel;
        var httpResponse = await httpClient.GetAsync(Options.IdpMetadataUrl, cancellationToken);
        var xml = await httpResponse.Content.ReadAsStringAsync();
        _entityDescriptor = new EntityDescriptor();
        _entityDescriptor = _entityDescriptor.ReadIdPSsoDescriptor(xml);
        return _entityDescriptor;
    }

    private class DefaultHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _httpClient;

        public DefaultHttpClientFactory(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public HttpClient CreateClient(string name) => _httpClient;
    }
}
