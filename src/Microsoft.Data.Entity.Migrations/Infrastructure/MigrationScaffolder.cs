// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        private readonly CSharpMigrationCodeGenerator _migrationGenerator;

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
            _namespace = @namespace;
            _directory = directory;

            _migrationGenerator
                = (CSharpMigrationCodeGenerator)servicesProvider.GetService(typeof(CSharpMigrationCodeGenerator)) 
                ?? new CSharpMigrationCodeGenerator();
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

        public virtual void ScaffoldMigration([NotNull] IMigrationMetadata migration)
        {
            Check.NotNull(migration, "migration");

            var stringBuilder = new IndentedStringBuilder();
            var designerStringBuilder = new IndentedStringBuilder();
            var className = GetClassName(migration);

            MigrationGenerator.GenerateClass(Namespace, className, migration, stringBuilder);
            MigrationGenerator.GenerateDesignerClass(Namespace, className, migration, designerStringBuilder);

            OnMigrationScaffolded(className, stringBuilder.ToString(), designerStringBuilder.ToString());
        }

        // TODO: Consider splitting model scaffolding to its own class.
        public virtual void ScaffoldModel([NotNull] IModel model)
        {
            Check.NotNull(model, "model");

            var stringBuilder = new IndentedStringBuilder();
            var className = GetClassName(model);

            MigrationGenerator.ModelGenerator.GenerateClass(Namespace, className, model, stringBuilder);

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

        protected virtual void OnMigrationScaffolded(string className, string migration, string metadata)
        {            
            var fileName = className + MigrationGenerator.CodeFileExtension;

            WriteFile(fileName, migration, FileMode.CreateNew);

            var designerFileName = className + ".Designer" + MigrationGenerator.CodeFileExtension;

            WriteFile(designerFileName, metadata, FileMode.Create);
        }

        protected virtual void OnModelScaffolded(string className, string model)
        {
            var fileName = className + MigrationGenerator.ModelGenerator.CodeFileExtension;

            WriteFile(fileName, model, FileMode.Create);
        }

        protected virtual void WriteFile(string fileName, string content, FileMode fileMode)
        {
#if NET45
            var filePath = Path.Combine(Directory, fileName);

            using (var stream = new FileStream(filePath, fileMode, FileAccess.Write))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(content);
                }
            }            
#endif
        }
    }
}
