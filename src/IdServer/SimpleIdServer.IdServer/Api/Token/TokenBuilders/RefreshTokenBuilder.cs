﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using SimpleIdServer.DPoP;
using SimpleIdServer.IdServer.DTOs;
using SimpleIdServer.IdServer.Helpers;
using SimpleIdServer.IdServer.Options;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleIdServer.IdServer.Api.Token.TokenBuilders
{
    public class RefreshTokenBuilder : ITokenBuilder
    {
        private readonly IGrantedTokenHelper _grantedTokenHelper;
        private readonly IdServerHostOptions _options;

        public RefreshTokenBuilder(IGrantedTokenHelper grantedTokenHelper, IOptions<IdServerHostOptions> options)
        {
            _grantedTokenHelper = grantedTokenHelper;
            _options = options.Value;
        }

        protected IGrantedTokenHelper GrantedTokenHelper => _grantedTokenHelper;
        public string Name => TokenResponseParameters.RefreshToken;

        public virtual async Task Build(BuildTokenParameter parameter, HandlerContext handlerContext, CancellationToken cancellationToken, bool useOriginalRequest = false)
        {
            using (var activity = Tracing.BasicActivitySource.StartActivity("BuildRefreshToken"))
            {
                var dic = new JsonObject();
                if (handlerContext.Request.RequestData != null)
                    foreach (var record in handlerContext.Request.RequestData)
                        if (record.Value is JsonValue)
                            dic.Add(record.Key, QueryCollectionExtensions.GetValue(record.Value.GetValue<string>()));
                        else
                            dic.Add(record.Key, QueryCollectionExtensions.GetValue(record.Value.ToJsonString()));

                if (handlerContext.User != null)
                    dic.Add(JwtRegisteredClaimNames.Sub, handlerContext.User.Name);

                var authorizationCode = string.Empty;
                handlerContext.Response.TryGet(AuthorizationResponseParameters.Code, out authorizationCode);
                string jkt = null;
                if (handlerContext.DPOPProof != null)
                    jkt = handlerContext.DPOPProof.PublicKey().CreateThumbprint();

                var sessionId = handlerContext.Session?.SessionId;
                var refreshToken = await GrantedTokenHelper.AddRefreshToken(
                    handlerContext.Client.ClientId,
                    authorizationCode,
                    parameter.GrantId,
                    dic,
                    handlerContext.OriginalRequest,
                    handlerContext.Client.RefreshTokenExpirationTimeInSeconds ?? _options.DefaultRefreshTokenExpirationTimeInSeconds,
                    jkt,
                    sessionId,
                    cancellationToken);
                handlerContext.Response.Add(TokenResponseParameters.RefreshToken, refreshToken);
            }
        }
    }
}
