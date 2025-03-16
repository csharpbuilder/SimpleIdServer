﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using SimpleIdServer.DPoP;
using SimpleIdServer.IdServer.Config;
using SimpleIdServer.IdServer.Exceptions;
using SimpleIdServer.IdServer.Options;
using SimpleIdServer.IdServer.Resources;
using System;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleIdServer.IdServer.Api.Token.Handlers
{
    public interface IDPOPProofValidator
    {
        Task Validate(HandlerContext context);
    }

    public class DPOPProofValidator : IDPOPProofValidator
    {
        private readonly IdServerHostOptions _options;
        private readonly IDistributedCache _distributedCache;

        public DPOPProofValidator(IOptions<IdServerHostOptions> options, IDistributedCache distributedCache)
        {
            _options = options.Value;
            _distributedCache = distributedCache;
        }

        public async Task Validate(HandlerContext context)
        {
            if (!context.Client.DPOPBoundAccessTokens) return;
            if (!context.Request.HttpHeader.ContainsKey(Constants.DPOPHeaderName)) throw new OAuthException(ErrorCodes.INVALID_REQUEST, Global.MissingDpopProof);
            var value = context.Request.HttpHeader[Constants.DPOPHeaderName];
            string dpopProof = null;
            if (value is JsonArray)
            {
                var values = value as JsonArray;
                if(values.Count > 1) throw new OAuthException(ErrorCodes.INVALID_REQUEST, Global.TooManyDpopHeader);
                dpopProof = values.First().AsValue().GetValue<string>();
            }
            else
            {
                if (!(value is JsonValue)) throw new OAuthException(ErrorCodes.INVALID_REQUEST, Global.InvalidDpopHeader);
                dpopProof = (context.Request.HttpHeader[Constants.DPOPHeaderName] as JsonValue).GetValue<string>();
            }

            var handler = new DPoPHandler();
            var validationResult = handler.Validate(dpopProof, DefaultTokenSecurityAlgs.AllSigAlgs, context.Request.HttpMethod, $"{context.GetIssuer()}/{Config.DefaultEndpoints.Token}", _options.DPoPLifetimeSeconds);
            if (!validationResult.IsValid) throw new OAuthException(ErrorCodes.INVALID_DPOP_PROOF, validationResult.ErrorMessage);
            await ValidateNonce(context, validationResult.Jwt);
            context.SetDPOPProof(validationResult.Jwt);
        }

        private async Task ValidateNonce(HandlerContext context, JsonWebToken jwt)
        {
            if (!context.Client.IsDPOPNonceRequired) return;
            bool isNonceValid = false;
            var nonce = jwt.Nonce();
            if(!string.IsNullOrWhiteSpace(nonce))
                isNonceValid = (await _distributedCache.GetAsync(nonce)) != null;

            if (isNonceValid)
            {
                await _distributedCache.RemoveAsync(nonce);
                return;
            }

            var newNonce = await CreateDPoPNonce(context.Client.DPOPNonceLifetimeInSeconds);
            throw new OAuthDPoPRequiredException(newNonce, ErrorCodes.USE_DPOP_NONCE, Global.UseDpopNonce);
        }

        private async Task<string> CreateDPoPNonce(double validityPeriodsInSeconds)
        {
            var nonce = Guid.NewGuid().ToString();
            await _distributedCache.SetAsync(nonce, Encoding.UTF8.GetBytes(nonce), new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromSeconds(validityPeriodsInSeconds)
            }, CancellationToken.None);
            return nonce;
        }
    }
}
