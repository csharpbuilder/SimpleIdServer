﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System;

namespace SimpleIdServer.Scim
{
    public class ScimHostOptions
    {
        public ScimHostOptions()
        {
            AuthenticationScheme = SCIMConstants.AuthenticationScheme;
            SCIMIdClaimName = "scim_id";
            MaxOperations = 1000;
            MaxPayloadSize = 1048576;
            MaxResults = 200;
            IgnoreUnsupportedCanonicalValues = true;
            IncludeToken = false;
            MergeExtensionAttributes = false;
            IsUserPublishEvtsEnabled = true;
            IsGroupPublishEvtsEnabled = true;
        }

        /// <summary>
        /// Authentication scheme.
        /// </summary>
        public string AuthenticationScheme { get; set; }
        /// <summary>
        /// Name of the claim used to get the scim identifier.
        /// </summary>
        public string SCIMIdClaimName { get; set; }
        /// <summary>
        /// An integer value specifying the maximum number of operations.
        /// </summary>
        public int MaxOperations { get; set; }
        /// <summary>
        /// An integer value specifying the maximum payload size in bytes.
        /// </summary>
        public int MaxPayloadSize { get; set; }
        /// <summary>
        /// An integer value specifying the maximum number of resources returned in a response.
        /// </summary>
        public int MaxResults { get; set; }
        /// <summary>
        /// Ignore unsupported canonical values. 
        /// If set to 'false' and the canonical value is not supported then an exception is thrown.
        /// </summary>
        public bool IgnoreUnsupportedCanonicalValues { get; set; }
        /// <summary>
        /// Include token in external events.
        /// </summary>
        public bool IncludeToken { get; set; }
        /// <summary>
        /// When this option is true then extension attributes will be merged into core attributes.
        /// </summary>
        public bool MergeExtensionAttributes { get; set; }
        /// <summary>
        /// Enable/Disable publishing user evts.
        /// </summary>
        public bool IsUserPublishEvtsEnabled { get; set; }
        /// <summary>
        /// Enable/Disable publishing group evts.
        /// </summary>
        public bool IsGroupPublishEvtsEnabled { get; set; }
        /// <summary>
        /// Function used to generate ServiceProviderConfig identifier.
        /// </summary>
        public Func<string> ServiceProviderConfigIdGenerator { get; set; } = () => Guid.NewGuid().ToString();
        /// <summary>
        /// Enable or disable bulk operations.
        /// </summary>
        public bool IsBulkEnabled { get; set; } = true;
        /// <summary>
        /// Set the application start datetime.
        /// </summary>
        public DateTime StartDateTime { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Set the service provider configuration identifier.
        /// </summary>
        public string ServiceProviderConfigId { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// Register to the events.
        /// </summary>
        public SCIMHostEvents SCIMEvents { get; set; } = new SCIMHostEvents();
        /// <summary>
        /// Enable or disable realm.
        /// </summary>
        internal bool EnableRealm { get; set; } = false;
        public bool IsFullRepresentationReturned { get; set; }
        internal bool IsBigMessagePublished { get; set; }
    }
}
