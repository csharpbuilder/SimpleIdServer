﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleIdServer.Configuration.Models;
using SimpleIdServer.IdServer.Domains;

namespace SimpleIdServer.IdServer.Store.Configurations;

public class ConfigurationKeyPairValueRecordConfiguration : IEntityTypeConfiguration<ConfigurationKeyPairValueRecord>
{
    public void Configure(EntityTypeBuilder<ConfigurationKeyPairValueRecord> builder)
    {
        builder.HasKey(b => b.Name);
    }
}
