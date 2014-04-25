// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations.Utilities;
using Microsoft.Data.Entity.Relational;

namespace Microsoft.Data.Entity.Migrations.Infrastructure
{
    public class HistoryRepository
    {
        private readonly Type _contextType;
        private readonly IServiceProvider _serviceProvider;        

        public HistoryRepository(
            [NotNull] Type contextType, [NotNull] IServiceProvider serviceProvider)
        {
            Check.NotNull(contextType, "contextType");
            Check.NotNull(serviceProvider, "serviceProvider");

            _contextType = contextType;
            _serviceProvider = serviceProvider;
        }

        public virtual Type ContextType
        {
            get { return _contextType; }
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

        public virtual IReadOnlyList<IMigrationMetadata> GetMigrations()
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
            var configuration = new DbContextOptions()
                .UseModel(CreateHistoryModel())
                .BuildConfiguration();

            return new DbContext(ServiceProvider, configuration);
        }

        public virtual string CreateContextKey()
        {
            return ContextType.Name;
        }

        private class HistoryRow
        {
            public string MigrationName { get; set; }
            public string Timestamp { get; set; }
            public string ContextKey { get; set; }            
        }
    }
}
