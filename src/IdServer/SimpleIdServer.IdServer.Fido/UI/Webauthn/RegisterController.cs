﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SimpleIdServer.IdServer.Domains;
using SimpleIdServer.IdServer.Fido.UI.ViewModels;
using SimpleIdServer.IdServer.Helpers;
using SimpleIdServer.IdServer.Jwt;
using SimpleIdServer.IdServer.Options;
using SimpleIdServer.IdServer.Stores;
using SimpleIdServer.IdServer.UI;
using System.Security.Claims;

namespace SimpleIdServer.IdServer.Fido.UI.Webauthn
{
    [Area(Constants.AMR)]
    public class RegisterController : BaseRegisterController<RegisterWebauthnViewModel>
    {
        public RegisterController(
            IOptions<IdServerHostOptions> options, 
            IDistributedCache distributedCache, 
            IUserRepository userRepository,
            ITokenRepository tokenRepository,
            ITransactionBuilder transactionBuilder,
            IJwtBuilder jwtBuilder,
            IRealmStore realmStore) : base(options, distributedCache, userRepository, tokenRepository, transactionBuilder, jwtBuilder, realmStore)
        {
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromRoute] string prefix, string? redirectUrl = null)
        {
            var issuer = Request.GetAbsoluteUriWithVirtualPath();
            if (!string.IsNullOrWhiteSpace(prefix))
                prefix = $"{prefix}/";
            var viewModel = new RegisterWebauthnViewModel();
            var isAuthenticated = User.Identity.IsAuthenticated;
            var userRegistrationProgress = await GetRegistrationProgress();
            if (userRegistrationProgress == null && !isAuthenticated)
            {
                viewModel.IsNotAllowed = true;
                return View(viewModel);
            }

            var login = string.Empty;
            if (!isAuthenticated)
            {
                login = userRegistrationProgress.User?.Name;
            }
            else login = User.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value;
            return View(new RegisterWebauthnViewModel
            {
                Login = login,
                BeginRegisterUrl = $"{issuer}/{prefix}{Constants.EndPoints.BeginRegister}",
                EndRegisterUrl = $"{issuer}/{prefix}{Constants.EndPoints.EndRegister}",
                Amr = userRegistrationProgress?.Amr,
                Steps = userRegistrationProgress?.Steps,
                RedirectUrl = userRegistrationProgress?.RedirectUrl ?? redirectUrl
            });
        }

        protected override void EnrichUser(User user, RegisterWebauthnViewModel viewModel) { }
    }
}
