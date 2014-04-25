// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.IO;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Data.Entity.Migrations.Utilities;

namespace Microsoft.Data.Entity.Migrations.Infrastructure
{
    public class MigrationScaffolder
    {
        private readonly Type _contextType;
        private readonly string _namespace;
        private readonly string _directory;
        // TODO: Create and use language agnostic abstraction if we plan to support anything other than CSharp.
        private CSharpMigrationCodeGenerator _migrationGenerator; 
        private CSharpModelCodeGenerator _modelGenerator;

        public MigrationScaffolder(
            [NotNull] Type contextType,
            [NotNull] string @namespace,
            [NotNull] string directory, 
            [NotNull] IServiceProvider servicesProvider)
        {
            Check.NotNull(contextType, "contextType");
            Check.NotEmpty(@namespace, "namespace");
            Check.NotEmpty(directory, "directory");            
            Check.NotNull(servicesProvider, "servicesProvider");

            _contextType = contextType;
            _directory = directory;
            _namespace = @namespace;

            _migrationGenerator
                = (CSharpMigrationCodeGenerator)servicesProvider.GetService(typeof(CSharpMigrationCodeGenerator)) 
                ?? new CSharpMigrationCodeGenerator();

            _modelGenerator 
                = (CSharpModelCodeGenerator)servicesProvider.GetService(typeof(CSharpModelCodeGenerator))  
                ?? new CSharpModelCodeGenerator();
        }

        public virtual Type ContextType
        {
            get { return _contextType; }
        }

        public virtual string Namespace
        {
            get { return _namespace; }
        }

        public virtual string Directory
        {
            get { return _directory; }
        }

        protected virtual CSharpMigrationCodeGenerator MigrationGenerator
        {
            get { return _migrationGenerator; }
        }

        protected virtual CSharpModelCodeGenerator ModelGenerator
        {
            get { return _modelGenerator; }
        }

        public virtual void ScaffoldMigration([NotNull] IMigrationMetadata migration)
        {
            Check.NotNull(migration, "migration");

            var stringBuilder = new IndentedStringBuilder();
            var className = GetClassName(migration);

            MigrationGenerator.GenerateClass(
                className, 
                Namespace,
                migration.UpgradeOperations,
                migration.DowngradeOperations, 
                stringBuilder);

            OnMigrationScaffolded(className, stringBuilder.ToString());
        }

        public virtual void ScaffoldModel([NotNull] IModel model)
        {
            Check.NotNull(model, "model");

            var stringBuilder = new IndentedStringBuilder();
            var className = GetClassName(model);

            ModelGenerator.GenerateClass(className, Namespace, model, stringBuilder);

            OnModelScaffolded(className, stringBuilder.ToString());
        }

        protected virtual string GetClassName([NotNull] IMigrationMetadata migration)
        {
            Check.NotNull(migration, "migration");

            return migration.Name;
        }

        protected virtual string GetClassName([NotNull] IModel model)
        {
            Check.NotNull(model, "model");

            return ContextType.Name + "ModelSnapshot";
        }

        protected virtual void OnMigrationScaffolded(string className, string text)
        {            
            var fileName = className + MigrationGenerator.CodeFileExtension;
            var filePath = Path.Combine(Directory, fileName);

            using (var writer = new StreamWriter(filePath))
            {
                writer.Write(text);
            }
        }

        protected virtual void OnModelScaffolded(string className, string text)
        {
            var fileName = className + ModelGenerator.CodeFileExtension;
            var filePath = Path.Combine(Directory, fileName);

            using (var writer = new StreamWriter(filePath))
            {
                writer.Write(text);
            }
        }
    }
}
