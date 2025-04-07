﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
namespace SimpleIdServer.Scim.Persistence.EF;

public class ScimEfOptions
{
    public string DefaultSchema { get; set; } = "dbo";
    public bool IgnoreBulkOperation { get; set; } = false;
}
