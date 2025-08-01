﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace SimpleIdServer.Scim.DTOs
{
    public class PatchRepresentationParameter : BaseParameter
    {
        /// <summary>
        /// Is an array of one or more PATCH operations.
        /// </summary>
        [DataMember(Name = SCIMConstants.PathOperationAttributes.Operations)]
        [JsonPropertyName(SCIMConstants.PathOperationAttributes.Operations)]
        public ICollection<PatchOperationParameter> Operations { get; set; }
    }
}
