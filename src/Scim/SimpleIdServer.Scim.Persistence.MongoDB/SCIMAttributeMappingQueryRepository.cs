﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using MongoDB.Driver;
using SimpleIdServer.Scim.Domains;
using SimpleIdServer.Scim.Persistence.MongoDB.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleIdServer.Scim.Persistence.MongoDB
{
    public class SCIMAttributeMappingQueryRepository : ISCIMAttributeMappingQueryRepository
    {   
        private readonly SCIMDbContext _scimDbContext;

        public SCIMAttributeMappingQueryRepository(SCIMDbContext scimDbContext)
        {
            _scimDbContext = scimDbContext;
        }

        public async Task<List<SCIMAttributeMapping>> GetAll()
        {
            var attributeMappings = _scimDbContext.SCIMAttributeMappingLst;
            return await attributeMappings.AsQueryable().ToMongoListAsync();
        }

        public async Task<List<SCIMAttributeMapping>> GetBySourceAttributes(IEnumerable<string> sourceAttributes)
        {
            var attributeMappings = _scimDbContext.SCIMAttributeMappingLst;
            return await attributeMappings.AsQueryable().Where(a => sourceAttributes.Contains(a.SourceAttributeSelector)).ToMongoListAsync();
        }

        public async Task<List<SCIMAttributeMapping>> GetBySourceResourceType(string sourceResourceType)
        {
            var attributeMappings = _scimDbContext.SCIMAttributeMappingLst;
            return await attributeMappings.AsQueryable().Where(a => a.SourceResourceType == sourceResourceType).ToMongoListAsync();
        }

        public async Task<List<SCIMAttributeMapping>> GetByTargetResourceType(string targetResourceType)
        {
            var attributeMappings = _scimDbContext.SCIMAttributeMappingLst;
            return await attributeMappings.AsQueryable().Where(a => a.TargetResourceType == targetResourceType).ToMongoListAsync();
        }
    }
}