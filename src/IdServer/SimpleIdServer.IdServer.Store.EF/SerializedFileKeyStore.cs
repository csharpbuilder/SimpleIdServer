﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore;
using SimpleIdServer.IdServer.Domains;
using SimpleIdServer.IdServer.Stores;
using SimpleIdServer.Scim.Domains;

namespace SimpleIdServer.IdServer.Store.EF;

public class SerializedFileKeyStore : IFileSerializedKeyStore
{
    private readonly StoreDbContext _dbContext;

    public SerializedFileKeyStore(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<SerializedFileKey>> GetByKeyIds(List<string> keyIds, CancellationToken cancellationToken)
    {
        return _dbContext.SerializedFileKeys
            .Include(s => s.Realms)
            .Where(s => keyIds.Contains(s.KeyId))
            .ToListAsync(cancellationToken);
    }

    public IQueryable<SerializedFileKey> Query() => _dbContext.SerializedFileKeys;

    public Task<List<SerializedFileKey>> GetAll(string realm, CancellationToken cancellationToken)
    {
        return _dbContext.SerializedFileKeys
            .Include(s => s.Realms)
            .Where(s => s.Realms.Any(r => r.Name == realm))
            .ToListAsync(cancellationToken);
    }

    public Task<List<SerializedFileKey>> GetAllSig(string realm, CancellationToken cancellationToken)
    {
        return _dbContext.SerializedFileKeys
            .Include(s => s.Realms)
            .Where(s => s.Usage == Constants.JWKUsages.Sig && s.Realms.Any(r => r.Name == realm))
            .ToListAsync(cancellationToken);
    }

    public Task<List<SerializedFileKey>> GetAllEnc(string realm, CancellationToken cancellationToken)
    {
        return _dbContext.SerializedFileKeys
            .Include(s => s.Realms)
            .Where(s => s.Usage == Constants.JWKUsages.Enc && s.Realms.Any(r => r.Name == realm))
            .ToListAsync(cancellationToken);
    }

    public void Add(SerializedFileKey key) => _dbContext.SerializedFileKeys.Add(key);

    public void Update(SerializedFileKey key)
    {

    }
}
