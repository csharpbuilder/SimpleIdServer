﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Fluxor;
using Fluxor.Blazor.Web.ReduxDevTools;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Radzen;
using SimpleIdServer.CredentialIssuer.Website;
using System.Security.Claims;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCredentialIssuerWebsite(
        this IServiceCollection services,
        Action<CredentialIssuerWebsiteOptions>? callbackOptions = null)
    {
        services.AddFluxor(o =>
        {
            o.ScanAssemblies(typeof(ServiceCollectionExtensions).Assembly);
            o.UseReduxDevTools(rdt =>
            {
                rdt.Name = "SimpleIdServer";
            });
        });
        services.AddTransient<SidCookieEventHandler>();
        services.AddScoped<DialogService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<ContextMenuService>();
        services.AddScoped<TooltipService>();
        services.AddSingleton<IWebsiteHttpClientFactory, WebsiteHttpClientFactory>();
        if (callbackOptions == null) services.Configure<CredentialIssuerWebsiteOptions>((o) => { });
        else services.Configure(callbackOptions);
        return services;
    }

    public static IServiceCollection AddDefaultSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        string defaultSecurityOptionsSectionName = nameof(DefaultSecurityOptions);
        DefaultSecurityOptions? defaultSecurityOptions = configuration.GetSection(defaultSecurityOptionsSectionName).Get<DefaultSecurityOptions>();
        if (defaultSecurityOptions == null)
        {
            Console.WriteLine($"Please configure the '{defaultSecurityOptionsSectionName}' section.");
            Environment.Exit(1);
        }

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = "oidc";
        })
        .AddCookie(options =>
        {
            options.Cookie.SameSite = SameSiteMode.None;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.HttpOnly = true;
            options.EventsType = typeof(SidCookieEventHandler);
        })
        .AddOpenIdConnect("oidc", config =>
        {
            if (defaultSecurityOptions.IgnoreCertificateError)
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
                    {
                        return true;
                    }
                };
                config.BackchannelHttpHandler = handler;
            }

            config.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
            config.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
            config.Authority = defaultSecurityOptions.Issuer;
            config.ClientId = defaultSecurityOptions.ClientId;
            config.ClientSecret = defaultSecurityOptions.ClientSecret;
            config.ResponseType = "code";
            config.ResponseMode = "query";
            config.SaveTokens = true;
            config.GetClaimsFromUserInfoEndpoint = true;
            config.RequireHttpsMetadata = false;
            config.MapInboundClaims = false;
            config.ClaimActions.MapJsonKey(ClaimTypes.Role, "role");
            config.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "name",
                RoleClaimType = "role"
            };
            config.Scope.Clear();
            if (!string.IsNullOrEmpty(defaultSecurityOptions.Scope))
            {
                string[] scopes = defaultSecurityOptions.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.RemoveEmptyEntries);
                foreach (string scope in scopes) config.Scope.Add(scope);
            }
        });

        services.AddAuthorization(config =>
        {
            var policyBuilder = new AuthorizationPolicyBuilder().RequireAuthenticatedUser();

            if (!string.IsNullOrEmpty(defaultSecurityOptions.RequiredRole))
            {
                string[] roles = defaultSecurityOptions.RequiredRole
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.RemoveEmptyEntries);
                policyBuilder.RequireRole(roles);
            }

            config.FallbackPolicy = policyBuilder.Build();
        });
        services.AddSingleton(defaultSecurityOptions);
        return services;
    }
}
