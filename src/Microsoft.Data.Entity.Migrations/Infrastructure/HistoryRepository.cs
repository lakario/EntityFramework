// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations.Utilities;
using Microsoft.Data.Entity.Relational;

namespace Microsoft.Data.Entity.Migrations.Infrastructure
{
    public class HistoryRepository
    {
        private readonly DbContext _context;
        private readonly IServiceProvider _serviceProvider;        

        public HistoryRepository(
            [NotNull] DbContext context, [NotNull] IServiceProvider serviceProvider)
        {
            Check.NotNull(context, "context");
            Check.NotNull(serviceProvider, "serviceProvider");

            _context = context;
            _serviceProvider = serviceProvider;
        }

        public virtual DbContext Context
        {
            get { return _context; }
        }

        public virtual IServiceProvider ServiceProvider
        {
            get { return _serviceProvider; }
        }

        public virtual SchemaQualifiedName TableName
        {
            // TODO: We probably need a DefaultSchema instead of hardcoding "dbo".
            get { return new SchemaQualifiedName("__MigrationHistory", "dbo"); }
        }

        public virtual IReadOnlyList<IMigrationMetadata> Migrations
        {
            get
            {
                using (var historyContext = CreateHistoryContext())
                {
                    return historyContext.Set<HistoryRow>()
                        .Where(h => h.ContextKey == CreateContextKey())
                        .Select(h => new MigrationMetadata(h.MigrationName, h.Timestamp))
                        .OrderBy(m => m.Timestamp)
                        .ToArray();
                }
            }
        }

        public virtual void AddMigration([NotNull] IMigrationMetadata migration)
        {
            Check.NotNull(migration, "migration");

            using (var historyContext = CreateHistoryContext())
            {
                historyContext.Set<HistoryRow>().Add(
                    new HistoryRow()
                        {
                            MigrationName = migration.Name,
                            Timestamp = migration.Timestamp,
                            ContextKey = CreateContextKey()
                        });

                historyContext.SaveChanges();
            }
        }

        public virtual IModel CreateHistoryModel()
        {
            var builder = new ModelBuilder();

            builder
                .Entity<HistoryRow>()
                .ToTable(TableName)
                .Properties(
                    ps =>
                    {
                        ps.Property(e => e.MigrationName);
                        ps.Property(e => e.Timestamp);
                        ps.Property(e => e.ContextKey);
                    })
                .Key(e => new { e.MigrationName, e.ContextKey });

            return builder.Model;
        }

        public virtual DbContext CreateHistoryContext()
        {
            var contextOptions = new DbContextOptions().UseModel(CreateHistoryModel());
            var extensions = Context.Configuration.ContextOptions.Extensions;

            foreach (var item in extensions)
            {
                var extension = item;
                contextOptions.AddBuildAction(c => c.AddOrUpdateExtension(extension));
            }

            return new DbContext(ServiceProvider, contextOptions.BuildConfiguration());
        }

        public virtual string CreateContextKey()
        {
            return Context.GetType().Name;
        }

        private class HistoryRow
        {
            public string MigrationName { get; set; }
            public string Timestamp { get; set; }
            public string ContextKey { get; set; }            
        }
    }
}
