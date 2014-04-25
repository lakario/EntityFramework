// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Migrations.Model;
using Microsoft.Data.Entity.Migrations.Utilities;
using Microsoft.Data.Entity.Relational;
using System.Globalization;

namespace Microsoft.Data.Entity.Migrations.Infrastructure
{
    public class Migrator
    {
        private readonly DbContext _context;
        private readonly MigratorConfiguration _configuration;
        private HistoryRepository _historyRepository;
        private MigrationAssembly _migrationAssembly;
        private MigrationScaffolder _migrationScaffolder;
        private ModelDiffer _modelDiffer;
        private MigrationOperationSqlGenerator _sqlGenerator;
        private SqlStatementExecutor _sqlExecutor;

        public Migrator([NotNull] DbContext context, [NotNull] MigratorConfiguration configuration)
        {
            Check.NotNull(context, "context");
            Check.NotNull(configuration, "configuration");

            _context = context;
            _configuration = configuration.EnsureDefaults(_context);

            _historyRepository 
                = (HistoryRepository)_configuration.ServiceProvider.GetService(typeof(HistoryRepository)) 
                ?? new HistoryRepository(_context.GetType(), _configuration.ServiceProvider);

            _migrationAssembly 
                = (MigrationAssembly)_configuration.ServiceProvider.GetService(typeof(MigrationAssembly)) 
                ?? new MigrationAssembly(_configuration.MigrationAssembly, _configuration.MigrationNamespace);

            _migrationScaffolder 
                = (MigrationScaffolder)_configuration.ServiceProvider.GetService(typeof(MigrationScaffolder)) 
                ?? new MigrationScaffolder(
                    _context.GetType(),
                    _configuration.MigrationNamespace,
                    _configuration.MigrationDirectory, 
                    _configuration.ServiceProvider);

            _modelDiffer 
                = (ModelDiffer)_configuration.ServiceProvider.GetService(typeof(ModelDiffer)) 
                ?? new ModelDiffer(new DatabaseBuilder());

            _sqlGenerator 
                = (MigrationOperationSqlGenerator)_configuration.ServiceProvider.GetService(typeof(MigrationOperationSqlGenerator))
                ?? new MigrationOperationSqlGenerator();

            _sqlExecutor
                = (SqlStatementExecutor)_configuration.ServiceProvider.GetService(typeof(SqlStatementExecutor))
                ?? new SqlStatementExecutor();
        }

        public virtual DbContext Context
        {
            get { return _context; }
        }

        public virtual MigratorConfiguration Configuration
        {
            get { return _configuration; }
        }

        protected virtual HistoryRepository HistoryRepository
        {
            get { return _historyRepository; }
        }

        protected virtual MigrationAssembly MigrationAssembly
        {
            get { return _migrationAssembly; }
        }

        protected virtual MigrationScaffolder MigrationScaffolder
        {
            get { return _migrationScaffolder; }
        }

        protected virtual ModelDiffer ModelDiffer
        {
            get { return _modelDiffer; }
        }

        protected virtual MigrationOperationSqlGenerator SqlGenerator
        {
            get { return _sqlGenerator; }
        }

        protected virtual SqlStatementExecutor SqlExecutor
        {
            get { return _sqlExecutor; }
        }

        public virtual void AddMigration([NotNull] string migrationName)
        {
            Check.NotEmpty(migrationName, "migrationName");

            // TODO: Handle duplicate migration names.

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssf", CultureInfo.InvariantCulture);
            var sourceModel = MigrationAssembly.Model;
            var targetModel = Context.Model;

            IReadOnlyList<MigrationOperation> upgradeOperations, downgradeOperations;
            if (sourceModel != null)
            {
                upgradeOperations = ModelDiffer.Diff(sourceModel, targetModel);
                downgradeOperations = ModelDiffer.Diff(targetModel, sourceModel);
            }
            else
            {
                upgradeOperations = ModelDiffer.DiffTarget(targetModel);
                downgradeOperations = ModelDiffer.DiffSource(targetModel);                
            }

            MigrationScaffolder.ScaffoldMigration(
                new MigrationMetadata(migrationName, timestamp)
                    {
                        SourceModel = MigrationAssembly.Model,
                        TargetModel = Context.Model,
                        UpgradeOperations = upgradeOperations,
                        DowngradeOperations = downgradeOperations
                    });
        }

        public virtual IReadOnlyList<IMigrationMetadata> GetPendingMigrations()
        {
            return MigrationAssembly.Migrations
                .Except(HistoryRepository.GetMigrations(), (x, y) => x.Name == y.Name)
                .ToArray();
        }

        public virtual void Upgrade()
        {
            foreach (var migration in GetPendingMigrations())
            {
                var statements = SqlGenerator.Generate(migration.UpgradeOperations, generateIdempotentSql: true);
                // TODO: Figure out what needs to be done to avoid the following cast.
                var connection = ((RelationalConnection)Context.Configuration.Connection).DbConnection;

                SqlExecutor.ExecuteNonQuery(connection, statements);
            }
        }
    }
}
