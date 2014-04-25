// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Migrations.Utilities;

namespace Microsoft.Data.Entity.Migrations.Infrastructure
{
    public class MigratorConfiguration
    {
        private Assembly _migrationAssembly;
        private string _migrationNamespace;
        private string _migrationDirectory;
        private IServiceProvider _serviceProvider;

        public virtual Assembly MigrationAssembly
        {
            get { return _migrationAssembly; }

            [param: NotNull]
            set { _migrationAssembly = Check.NotNull(value, "value"); }
        }

        public virtual string MigrationNamespace
        {
            get { return _migrationNamespace; }

            [param: NotNull]
            set { _migrationNamespace = Check.NotEmpty(value, "value"); }
        }

        public virtual string MigrationDirectory
        {
            get { return _migrationDirectory; }

            [param: NotNull]
            set { _migrationDirectory = Check.NotEmpty(value, "value"); }
        }

        public virtual IServiceProvider ServiceProvider
        {
            get { return _serviceProvider; }

            [param: NotNull]
            set { _serviceProvider = Check.NotNull(value, "value"); }
        }

        internal virtual MigratorConfiguration EnsureDefaults(DbContext context)
        {
            var contextType = context.GetType();

            if (_migrationAssembly == null)
            {
                _migrationAssembly = contextType.GetTypeInfo().Assembly;
            }

            if (_migrationNamespace == null)
            {
                _migrationNamespace = contextType.Namespace + ".Migrations";
            }

            if (_serviceProvider == null)
            {
                _serviceProvider = context.Configuration.Services.ServiceProvider;
            }

            return this;
        }
    }
}
