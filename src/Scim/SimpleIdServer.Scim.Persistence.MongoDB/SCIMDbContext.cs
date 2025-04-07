﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using SimpleIdServer.Scim.Domains;
using SimpleIdServer.Scim.Persistence.MongoDB.Models;
using System;

namespace SimpleIdServer.Scim.Persistence.MongoDB
{
    public class SCIMDbContext : IDisposable
	{
		private readonly MongoDbOptions _options;

		public SCIMDbContext(IMongoDatabase database, IOptions<MongoDbOptions> options)
		{
			Database = database;
			_options = options.Value;
		}

		internal IMongoDatabase Database { get; private set; }
		internal MongoDbOptions Options => _options;

		public IMongoCollection<SCIMRepresentationModel> SCIMRepresentationLst =>
			Database.GetCollection<SCIMRepresentationModel>(_options.CollectionRepresentations);

		public IMongoCollection<SCIMRepresentationAttribute> SCIMRepresentationAttributeLst
			=> Database.GetCollection<SCIMRepresentationAttribute>(_options.CollectionRepresentationAttributes);

		public IMongoCollection<SCIMSchema> SCIMSchemaLst =>
			Database.GetCollection<SCIMSchema>(_options.CollectionSchemas);

		public IMongoCollection<SCIMAttributeMapping> SCIMAttributeMappingLst =>
			Database.GetCollection<SCIMAttributeMapping>(_options.CollectionMappings);

		public IMongoCollection<ProvisioningConfiguration> ProvisioningConfigurationLst =>
			Database.GetCollection<ProvisioningConfiguration>(_options.CollectionProvisioningLst);

		public IMongoCollection<Realm> Realms =>
			Database.GetCollection<Realm>(_options.CollectionRealms);

		public void Dispose() { }

		internal static void RegisterMappings()
        {
            BsonClassMap.RegisterClassMap<Realm>(cm =>
            {
                cm.AutoMap();
				cm.MapIdMember(c => c.Name);
            });
            BsonClassMap.RegisterClassMap<ProvisioningConfiguration>(cm =>
			{
				cm.AutoMap();
			});
			BsonClassMap.RegisterClassMap<SCIMAttributeMapping>(cm =>
			{
				cm.AutoMap();
			});
			BsonClassMap.RegisterClassMap<SCIMRepresentationAttribute>(cm =>
			{
				cm.AutoMap();
				cm.UnmapMember(c => c.CachedChildren);
			});
            BsonClassMap.RegisterClassMap<SCIMRepresentation>(cm =>
			{
				cm.AutoMap();
				cm.SetIsRootClass(true);
				cm.UnmapMember(c => c.Schemas);
				cm.UnmapMember(c => c.HierarchicalAttributes);
                cm.UnmapMember(c => c.FlatAttributes);
            });
			BsonClassMap.RegisterClassMap<SCIMRepresentationModel>(cm =>
			{
				cm.AutoMap();
			});
			BsonClassMap.RegisterClassMap<SCIMSchema>(cm =>
			{
				cm.AutoMap();
				cm.UnmapMember(c => c.Representations);
				cm.UnmapMember(c => c.HierarchicalAttributes);
			});
			BsonClassMap.RegisterClassMap<SCIMSchemaAttribute>(cm =>
			{
				cm.AutoMap();
			});
		}
	}
}
