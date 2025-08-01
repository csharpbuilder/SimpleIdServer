﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.Extensions.Caching.Distributed;
using SimpleIdServer.IdServer.Domains;
using SimpleIdServer.IdServer.Fido.Apis;
using SimpleIdServer.IdServer.Fido.UI.ViewModels;
using SimpleIdServer.IdServer.Helpers;
using SimpleIdServer.IdServer.Layout.AuthFormLayout;
using SimpleIdServer.IdServer.Stores;
using SimpleIdServer.IdServer.UI.Services;
using System.Text.Json;

namespace SimpleIdServer.IdServer.Fido.Services
{
    public interface IWebauthnAuthenticationService : IUserAuthenticationService
    {

    }

    public class UserWebauthnAuthenticationService : GenericAuthenticationService<AuthenticateWebauthnViewModel>, IWebauthnAuthenticationService
    {
        private readonly IDistributedCache _distributedCache;

        public UserWebauthnAuthenticationService(IDistributedCache distributedCache, IAuthenticationHelper authenticationHelper, IUserRepository userRepository) : base(authenticationHelper, userRepository)
        {
            _distributedCache = distributedCache;
        }

        public override string Amr => Constants.AMR;

        protected override async Task<User> GetUser(string authenticatedUserId, AuthenticateWebauthnViewModel viewModel, string realm, CancellationToken cancellationToken)
        {
            User authenticatedUser = null;
            if (string.IsNullOrWhiteSpace(authenticatedUserId))
                authenticatedUser = await AuthenticateUser(viewModel.Login, realm, cancellationToken);
            else
                authenticatedUser = await FetchAuthenticatedUser(realm, authenticatedUserId, cancellationToken);

            return authenticatedUser;
        }

        protected override async Task<CredentialsValidationResult> Validate(string realm, string authenticatedUserId, AuthenticateWebauthnViewModel viewModel, CancellationToken cancellationToken)
        {
            var authenticatedUser = await GetUser(authenticatedUserId, viewModel, realm, cancellationToken);
            if (authenticatedUser == null) return CredentialsValidationResult.Error(ValidationStatus.UNKNOWN_USER);
            return await Validate(realm, authenticatedUser, viewModel, cancellationToken);
        }

        protected override async Task<CredentialsValidationResult> Validate(string realm, User authenticatedUser, AuthenticateWebauthnViewModel viewModel, CancellationToken cancellationToken)
        {
            if (authenticatedUser.IsBlocked()) return CredentialsValidationResult.Error(AuthFormErrorMessages.UserBlocked, AuthFormErrorMessages.UserBlocked);
            if (!authenticatedUser.GetStoredFidoCredentials(Constants.AMR).Any()) return CredentialsValidationResult.Error(AuthFormErrorMessages.MissingCredential, AuthFormErrorMessages.MissingCredential);
            var session = await _distributedCache.GetStringAsync(viewModel.SessionId, cancellationToken);
            if (string.IsNullOrWhiteSpace(session))
            {
                return CredentialsValidationResult.Error(AuthFormErrorMessages.UnknownSession, AuthFormErrorMessages.UnknownSession);
            }

            var sessionRecord = JsonSerializer.Deserialize<AuthenticationSessionRecord>(session);
            if (!sessionRecord.IsValidated)
            {
                return CredentialsValidationResult.Error(AuthFormErrorMessages.SessionNotValidated, AuthFormErrorMessages.SessionNotValidated);
            }

            return CredentialsValidationResult.Ok(authenticatedUser, false);
        }
    }
}
