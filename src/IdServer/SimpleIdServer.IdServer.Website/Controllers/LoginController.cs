﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SimpleIdServer.IdServer.Website.ViewModels;
using System.Text.Json.Nodes;

namespace SimpleIdServer.IdServer.Website.Controllers;

public class LoginController : Controller
{
    private readonly DefaultSecurityOptions _defaultSecurityOptions;
    private readonly IdServerWebsiteOptions _options;
    private readonly ILogger<LoginController> _logger;

    public LoginController(
        DefaultSecurityOptions defaultSecurityOptions,
        IOptions<IdServerWebsiteOptions> options,
        ILogger<LoginController> logger)
    {
        _defaultSecurityOptions = defaultSecurityOptions;
        _options = options.Value;
        _logger = logger;
    }

    [Route("auth")]
    public IActionResult Index(string returnUrl)
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = returnUrl
        };
        return Challenge(props, "oidc");
    }

    [Route("login")]
    public IActionResult Login(string acrValues, string realm = null)
    {
        var items = new Dictionary<string, string>
        {
            { "scheme", "oidc" },
        };
        var props = new AuthenticationProperties(items);
        props.SetParameter(OpenIdConnectParameterNames.Prompt, "login");
        props.SetParameter(OpenIdConnectParameterNames.AcrValues, acrValues);
        props.SetParameter(OpenIdConnectParameterNames.RedirectUri, GetRedirectUri(realm));
        var result = Challenge(props, "oidc");
        return result;
    }

    [Route("callback")]
    public async Task<IActionResult> Callback(string realm)
    {
        _logger.LogInformation("Execute callback");
        var issuer = _defaultSecurityOptions.Issuer;
        if(_options.IsReamEnabled) issuer = $"{issuer}/{realm}";
        var tokenEndpoint = $"{issuer}/token";
        var userInfoEndpoint = $"{issuer}/userinfo";
        var authorizationResponse = await ExtractAuthorizationResponse();
        var tokenResponse = await GetToken(realm, authorizationResponse, tokenEndpoint);
        var userInfo = await GetUserInfo(tokenResponse, userInfoEndpoint);
        return View(new CallbackViewModel { Claims = userInfo });
    }

    private async Task<OpenIdConnectMessage> ExtractAuthorizationResponse()
    {
        OpenIdConnectMessage message = null;
        if (HttpMethods.IsGet(Request.Method))
        {
            var queries = Request.Query.Select(pair => new KeyValuePair<string, string[]>(pair.Key, pair.Value.ToArray()));
            message = new OpenIdConnectMessage(queries);
        }
        else if(HttpMethods.IsPost(Request.Method) && !string.IsNullOrEmpty(Request.ContentType)
          && Request.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
          && Request.Body.CanRead)
        {
            var form = await Request.ReadFormAsync();
            var queries = form.Select(pair => new KeyValuePair<string, string[]>(pair.Key, pair.Value.ToArray()));
            message = new OpenIdConnectMessage(queries);
        }

        return message;
    }

    private async Task<OpenIdConnectMessage> GetToken(string realm, OpenIdConnectMessage authorizationResponse, string tokenEndpoint)
    {
        var redirectUri = GetRedirectUri(realm);
        using (var httpClient = BuildHttpClient())
        {
            var tokenEndpointRequest = new OpenIdConnectMessage
            {
                ClientId = _defaultSecurityOptions.ClientId,
                Code = authorizationResponse.Code,
                GrantType = OpenIdConnectGrantTypes.AuthorizationCode,
                RedirectUri = redirectUri,
                ClientSecret = _defaultSecurityOptions.ClientSecret
            };
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
            requestMessage.Content = new FormUrlEncodedContent(tokenEndpointRequest.Parameters);
            var httpResult = await httpClient.SendAsync(requestMessage);
            var json = await httpResult.Content.ReadAsStringAsync();
            return new OpenIdConnectMessage(json);
        }
    }

    private async Task<Dictionary<string, string>> GetUserInfo(OpenIdConnectMessage token, string userInfoEndpoint)
    {
        using(var httpClient = BuildHttpClient())
        {
            var request = new HttpRequestMessage { Method = HttpMethod.Get, RequestUri = new Uri(userInfoEndpoint) };
            request.Headers.Add("Authorization", $"Bearer {token.AccessToken}");
            var httpResult = await httpClient.SendAsync(request);
            var json = await httpResult.Content.ReadAsStringAsync();
            var jObj = JsonObject.Parse(json).AsObject();
            var result = new Dictionary<string, string>();
            foreach(var record in jObj)
            {
                if (record.Value == null) continue;
                result.Add(record.Key, record.Value.ToString());
            }

            return result;
        }
    }

    private HttpClient BuildHttpClient()
    {
        if (!_defaultSecurityOptions.IgnoreCertificateError) return new HttpClient();
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
            {
                return true;
            }
        };
        return new HttpClient(handler);
    }

    private string GetRedirectUri(string realm)
    {
        var issuer = Request.GetAbsoluteUriWithVirtualPath();
        return $"{issuer}/callback?realm={realm}";
    }
}
