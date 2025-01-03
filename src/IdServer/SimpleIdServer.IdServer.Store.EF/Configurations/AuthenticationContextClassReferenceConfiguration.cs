﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleIdServer.IdServer.Domains;

namespace SimpleIdServer.IdServer.Store.Configurations
{
    public class AuthenticationContextClassReferenceConfiguration : IEntityTypeConfiguration<AuthenticationContextClassReference>
    {
        public void Configure(EntityTypeBuilder<AuthenticationContextClassReference> builder)
        {
            builder.HasKey(c => c.Id);
            builder.HasMany(a => a.Realms).WithMany(a => a.AuthenticationContextClassReferences);
        }
    }
}
