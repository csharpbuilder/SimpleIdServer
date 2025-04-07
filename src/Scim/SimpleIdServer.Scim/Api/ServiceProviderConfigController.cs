﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleIdServer.Scim.Domain;
using SimpleIdServer.Scim.Domains;
using SimpleIdServer.Scim.Domains.Builders;
using SimpleIdServer.Scim.Helpers;
using SimpleIdServer.Scim.Resources;
using System.Collections.Generic;
using System.Net;

namespace SimpleIdServer.Scim.Api
{
    public class ServiceProviderConfigController : Controller
    {
        private readonly ScimHostOptions _options;
        private readonly IUriProvider _uriProvider;
        private readonly ILogger _logger;
        private readonly IRepresentationVersionBuilder _representationVersionBuilder;

        public ServiceProviderConfigController(IOptionsMonitor<ScimHostOptions> options, IUriProvider uriProvider, ILogger<ServiceProviderConfigController> logger, IRepresentationVersionBuilder representationVersionBuilder)
        {
            _options = options.CurrentValue;
            _uriProvider = uriProvider;
            _logger = logger;
            _representationVersionBuilder = representationVersionBuilder;
        }

        /// <summary>
        /// Returns the service provider's configuration details.
        /// </summary>
        /// <response code="200">Service provider's configuration details are found</response>
        /// <returns></returns>
        [ProducesResponseType(200)]
        [HttpGet]
        public virtual IActionResult Get()
        {
            _logger.LogInformation(Global.StartGetServiceProviderConfig);
            var schema = StandardSchemas.ServiceProvideConfigSchemas;
            var representation = SCIMRepresentationBuilder.Create(new List<SCIMSchema> { schema }, _options.ServiceProviderConfigIdGenerator == null ? null : _options.ServiceProviderConfigIdGenerator())
                .AddComplexAttribute("patch", schema.Id, c =>
                {
                    c.AddBooleanAttribute("supported", new List<bool> { true });
                })
                .AddComplexAttribute("bulk", schema.Id, c =>
                 {
                     c.AddBooleanAttribute("supported", new List<bool> { _options.IsBulkEnabled });
                     c.AddIntegerAttribute("maxOperations", new List<int> { _options.MaxOperations });
                     c.AddIntegerAttribute("maxPayloadSize", new List<int> { _options.MaxPayloadSize });
                 })
                .AddComplexAttribute("filter", schema.Id, c =>
                 {
                     c.AddBooleanAttribute("supported", new List<bool> { true });
                     c.AddIntegerAttribute("maxResults", new List<int> { _options.MaxResults });
                 })
                .AddComplexAttribute("changePassword", schema.Id, c =>
                 {
                     c.AddBooleanAttribute("supported", new List<bool> { false });
                 })
                .AddComplexAttribute("sort", schema.Id, c =>
                {
                    c.AddBooleanAttribute("supported", new List<bool> { false });
                })
                .AddComplexAttribute("etag", schema.Id, c =>
                {
                    c.AddBooleanAttribute("supported", new List<bool> { false });
                })
                .AddComplexAttribute("authenticationSchemes", schema.Id, c =>
                {
                    c.AddStringAttribute("name", new List<string> { "OAuth Bearer Token" });
                    c.AddStringAttribute("description", new List<string> { "Authentication scheme using the OAuth Bearer Token Standard" });
                    c.AddStringAttribute("specUri", new List<string> { "http://www.rfc-editor.org/info/rfc6750" });
                    c.AddStringAttribute("type", new List<string> { "oauthbearertoken" });
                    c.AddBooleanAttribute("primary", new List<bool> { true });
                }).Build();
            representation.Created = _options.StartDateTime;
            representation.SetUpdated(_options.StartDateTime, _representationVersionBuilder.Build(representation));
            representation.SetResourceType(SCIMResourceTypes.ServiceProviderConfig);
            representation.Id = _options.ServiceProviderConfigId;
            var location = $"{_uriProvider.GetAbsoluteUriWithVirtualPath()}/{SCIMEndpoints.ServiceProviderConfig}";
            return new ContentResult
            {
                StatusCode = (int)HttpStatusCode.OK,
                Content = representation.ToResponse(location, true).ToString(),
                ContentType = "application/scim+json"
            };
        }
    }
}
