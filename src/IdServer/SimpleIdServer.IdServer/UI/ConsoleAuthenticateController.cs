﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using FormBuilder;
using FormBuilder.Repositories;
using FormBuilder.Stores;
using MassTransit;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SimpleIdServer.IdServer.Api;
using SimpleIdServer.IdServer.Console;
using SimpleIdServer.IdServer.Console.Services;
using SimpleIdServer.IdServer.Helpers;
using SimpleIdServer.IdServer.Jwt;
using SimpleIdServer.IdServer.Options;
using SimpleIdServer.IdServer.Stores;
using SimpleIdServer.IdServer.UI.Infrastructures;
using SimpleIdServer.IdServer.UI.Services;
using SimpleIdServer.IdServer.UI.ViewModels;
using System.Collections.Generic;

namespace SimpleIdServer.IdServer.UI;

[Area(Constants.ConsoleAmr)]
public class ConsoleAuthenticateController : BaseOTPAuthenticateController<AuthenticateConsoleViewModel>
{
    private readonly IConfiguration _configuration;

    public ConsoleAuthenticateController(
        IConfiguration configuration,
        IEnumerable<IUserNotificationService> notificationServices, 
        IEnumerable<IOTPAuthenticator> otpAuthenticators,
        IUserConsoleAuthenticationService userAuthenticationService, 
        IAuthenticationSchemeProvider authenticationSchemeProvider, 
        IOptions<IdServerHostOptions> options,
        IDataProtectionProvider dataProtectionProvider, 
        IAuthenticationHelper authenticationHelper,
        IClientRepository clientRepository, 
        IAmrHelper amrHelper, 
        IUserRepository userRepository, 
        IUserSessionResitory userSessionRepository, 
        IUserTransformer userTransformer, 
        ITokenRepository tokenRepository, 
        ITransactionBuilder transactionBuilder,
        IJwtBuilder jwtBuilder, 
        IBusControl busControl,
        IAntiforgery antiforgery,
        IAuthenticationContextClassReferenceRepository authenticationContextClassReferenceRepository,
        ISessionManager sessionManager,
        IWorkflowStore workflowStore,
        IFormStore formStore,
        ILanguageRepository languageRepository,
        IAcrHelper acrHelper,
        IOptions<FormBuilderOptions> formBuilderOptions) : base(configuration, notificationServices, otpAuthenticators, userAuthenticationService, authenticationSchemeProvider, options, dataProtectionProvider, authenticationHelper, clientRepository, amrHelper, userRepository, userSessionRepository, userTransformer, tokenRepository, transactionBuilder, jwtBuilder, busControl, antiforgery, authenticationContextClassReferenceRepository, sessionManager, workflowStore, formStore, languageRepository, acrHelper, formBuilderOptions)
    {
        _configuration = configuration;
    }

    protected override string FormattedMessage => GetOptions()?.HttpBody;

    protected override string Amr => Constants.ConsoleAmr;

    protected override bool IsExternalIdProvidersDisplayed => false;

    protected override bool TryGetLogin(AcrAuthInfo amrInfo, out string login)
    {
        login = amrInfo.Login;
        return true;
    }

    private IdServerConsoleOptions GetOptions()
    {
        var section = _configuration.GetSection(typeof(IdServerConsoleOptions).Name);
        return section.Get<IdServerConsoleOptions>();
    }
}
